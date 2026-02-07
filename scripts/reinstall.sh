#!/bin/bash
# Uninstall, build, pack, and reinstall FilmStruck CLI for testing

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/FilmStruck.Cli"

echo "Uninstalling existing FilmStruck.Cli..."
dotnet tool uninstall --global FilmStruck.Cli 2>/dev/null || true

echo "Building..."
dotnet build "$PROJECT_DIR/FilmStruck.Cli.csproj"

echo "Packing..."
dotnet pack "$PROJECT_DIR/FilmStruck.Cli.csproj" --output "$PROJECT_DIR/nupkg"

echo "Installing..."
dotnet tool install --global --add-source "$PROJECT_DIR/nupkg" FilmStruck.Cli

echo "Done! Run 'filmstruck --help' to verify."
