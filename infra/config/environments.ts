export interface EnvironmentConfig {
  environment: string;
  tableName: string;
  lambdaMemoryMb: number;
  region: string;
  domain: string;
  account?: string;
  removalPolicy: 'destroy' | 'retain';
}

const environments: Record<string, EnvironmentConfig> = {
  staging: {
    environment: 'staging',
    tableName: 'filmstruck-staging',
    lambdaMemoryMb: 128,
    region: 'us-east-1',
    domain: 'staging.filmstruck.net',
    removalPolicy: 'destroy',
  },
  prod: {
    environment: 'prod',
    tableName: 'filmstruck-prod',
    lambdaMemoryMb: 256,
    region: 'us-east-1',
    domain: 'filmstruck.net',
    removalPolicy: 'retain',
  },
};

export function getEnvironmentConfig(envName: string): EnvironmentConfig {
  const config = environments[envName];
  if (!config) {
    throw new Error(
      `Unknown environment: ${envName}. Valid environments: ${Object.keys(environments).join(', ')}`
    );
  }
  return config;
}
