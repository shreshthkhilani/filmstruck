#!/bin/bash
# Clean build artifacts

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/.."

echo "Cleaning build artifacts..."

rm -rf "$REPO_ROOT/src/FilmStruck.Cli/bin"
rm -rf "$REPO_ROOT/src/FilmStruck.Cli/obj"
rm -rf "$REPO_ROOT/src/FilmStruck.Cli/nupkg"
rm -rf "$REPO_ROOT/test"

echo "Done!"
