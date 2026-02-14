.PHONY: build test unit-test integration-test clean reinstall help \
       api-build api-run infra-install infra-synth infra-deploy-staging infra-deploy-prod local-up

build:
	dotnet build src/FilmStruck.Cli/FilmStruck.Cli.csproj

test: unit-test integration-test

unit-test:
	dotnet test tests/FilmStruck.Cli.Tests/FilmStruck.Cli.Tests.csproj --verbosity normal

integration-test:
	./scripts/test.sh

clean:
	./scripts/clean.sh

reinstall:
	./scripts/reinstall.sh

api-build:
	dotnet build src/FilmStruck.Api/FilmStruck.Api.csproj

api-run:
	AWS_ENDPOINT_URL=http://localhost:8000 TABLE_NAME=filmstruck-staging \
		dotnet run --project src/FilmStruck.Api/FilmStruck.Api.csproj

infra-install:
	cd infra && npm install

infra-synth:
	cd infra && npx cdk synth -c env=staging

infra-deploy-staging:
	dotnet publish src/FilmStruck.Api/FilmStruck.Api.csproj -c Release -r linux-x64 --self-contained
	cd infra && npx cdk deploy -c env=staging

infra-deploy-prod:
	dotnet publish src/FilmStruck.Api/FilmStruck.Api.csproj -c Release -r linux-x64 --self-contained
	cd infra && npx cdk deploy -c env=prod

local-up:
	docker compose up -d
	./scripts/local-setup.sh

help:
	@echo "make build              - Build the CLI"
	@echo "make test               - Run all tests (unit + integration)"
	@echo "make unit-test          - Run unit tests only"
	@echo "make integration-test   - Run integration tests only"
	@echo "make clean              - Clean build artifacts"
	@echo "make reinstall          - Reinstall CLI globally"
	@echo "make api-build          - Build the API project"
	@echo "make api-run            - Run API locally (needs DynamoDB Local)"
	@echo "make infra-install      - Install CDK dependencies"
	@echo "make infra-synth        - Synthesize CloudFormation template"
	@echo "make infra-deploy-staging - Deploy to staging"
	@echo "make infra-deploy-prod  - Deploy to production"
	@echo "make local-up           - Start DynamoDB Local + seed data"
