import * as path from 'path';
import * as cdk from 'aws-cdk-lib';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
import * as route53 from 'aws-cdk-lib/aws-route53';
import * as route53Targets from 'aws-cdk-lib/aws-route53-targets';
import * as acm from 'aws-cdk-lib/aws-certificatemanager';
import { Construct } from 'constructs';
import { EnvironmentConfig } from '../config/environments';

export interface FilmStruckStackProps extends cdk.StackProps {
  config: EnvironmentConfig;
}

export class FilmStruckStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: FilmStruckStackProps) {
    super(scope, id, props);

    const { config } = props;

    // DynamoDB table
    const table = new dynamodb.Table(this, 'FilmStruckTable', {
      tableName: config.tableName,
      partitionKey: { name: 'PartitionKey', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'SortKey', type: dynamodb.AttributeType.STRING },
      billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
      removalPolicy:
        config.removalPolicy === 'destroy'
          ? cdk.RemovalPolicy.DESTROY
          : cdk.RemovalPolicy.RETAIN,
    });

    // Lambda function
    const apiProjectDir = path.join(__dirname, '../../src/FilmStruck.Api');
    const fn = new lambda.Function(this, 'ApiFunction', {
      runtime: lambda.Runtime.DOTNET_8,
      handler: 'FilmStruck.Api',
      code: lambda.Code.fromAsset(apiProjectDir, {
        bundling: {
          image: lambda.Runtime.DOTNET_8.bundlingImage,
          command: [
            'bash', '-c',
            'dotnet publish -c Release -r linux-x64 --self-contained -o /asset-output',
          ],
          user: 'root',
        },
      }),
      memorySize: config.lambdaMemoryMb,
      timeout: cdk.Duration.seconds(30),
      environment: {
        TABLE_NAME: config.tableName,
      },
    });

    table.grantReadData(fn);

    // API Gateway
    const api = new apigateway.RestApi(this, 'FilmStruckApi', {
      restApiName: `filmstruck-api-${config.environment}`,
      description: `FilmStruck API (${config.environment})`,
      defaultCorsPreflightOptions: {
        allowOrigins: apigateway.Cors.ALL_ORIGINS,
        allowMethods: apigateway.Cors.ALL_METHODS,
        allowHeaders: ['Content-Type', 'Authorization'],
      },
    });

    const apiResource = api.root.addResource('api');
    const usernameResource = apiResource.addResource('{username}');
    usernameResource.addMethod('GET', new apigateway.LambdaIntegration(fn));

    // Route 53 hosted zone (lookup existing)
    const hostedZone = route53.HostedZone.fromLookup(this, 'HostedZone', {
      domainName: 'filmstruck.net',
    });

    // ACM certificate
    const certificate = new acm.Certificate(this, 'Certificate', {
      domainName: 'filmstruck.net',
      subjectAlternativeNames: ['*.filmstruck.net'],
      validation: acm.CertificateValidation.fromDns(hostedZone),
    });

    // API Gateway custom domain
    const customDomain = new apigateway.DomainName(this, 'CustomDomain', {
      domainName: config.domain,
      certificate,
      endpointType: apigateway.EndpointType.EDGE,
    });

    customDomain.addBasePathMapping(api);

    // Route 53 alias records
    new route53.ARecord(this, 'AliasRecord', {
      zone: hostedZone,
      recordName: config.domain,
      target: route53.RecordTarget.fromAlias(
        new route53Targets.ApiGatewayDomain(customDomain)
      ),
    });

    new route53.AaaaRecord(this, 'AliasRecordAAAA', {
      zone: hostedZone,
      recordName: config.domain,
      target: route53.RecordTarget.fromAlias(
        new route53Targets.ApiGatewayDomain(customDomain)
      ),
    });

    // Outputs
    new cdk.CfnOutput(this, 'ApiUrl', {
      value: api.url,
      description: 'API Gateway URL',
    });

    new cdk.CfnOutput(this, 'TableName', {
      value: table.tableName,
      description: 'DynamoDB table name',
    });

    new cdk.CfnOutput(this, 'CustomDomainUrl', {
      value: `https://${config.domain}`,
      description: 'Custom domain URL',
    });
  }
}
