#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from 'aws-cdk-lib';
import { FilmStruckStack } from '../lib/filmstruck-stack';
import { getEnvironmentConfig } from '../config/environments';

const app = new cdk.App();

// Get environment from context or default to 'dev'
const envName = app.node.tryGetContext('env') || 'dev';
const config = getEnvironmentConfig(envName);

new FilmStruckStack(app, `FilmStruck-${config.environment}`, {
  config,
  env: {
    account: config.account || process.env.CDK_DEFAULT_ACCOUNT,
    region: config.region || process.env.CDK_DEFAULT_REGION,
  },
  description: `FilmStruck web application infrastructure (${config.environment})`,
});

app.synth();
