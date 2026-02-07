.PHONY: build test unit-test integration-test clean reinstall release help

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

release:
	@test -n "$(VERSION)" || (echo "Usage: make release VERSION=1.3.0" && exit 1)
	./scripts/prepare-release.sh $(VERSION)

help:
	@echo "make build            - Build the CLI"
	@echo "make test             - Run all tests (unit + integration)"
	@echo "make unit-test        - Run unit tests only"
	@echo "make integration-test - Run integration tests only"
	@echo "make clean            - Clean build artifacts"
	@echo "make reinstall        - Reinstall CLI globally"
	@echo "make release          - Prepare release (VERSION=x.y.z)"
