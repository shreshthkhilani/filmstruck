#!/bin/bash
# Prepare a new release: update version, changelog, create branch and PR

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/.."
CSPROJ="$REPO_ROOT/src/FilmStruck.Cli/FilmStruck.Cli.csproj"
CHANGELOG="$REPO_ROOT/CHANGELOG.md"

# Get version from argument or prompt
VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    read -p "Enter new version (e.g., 1.3.0): " VERSION
fi

# Validate version format
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: Invalid version format. Use semver (e.g., 1.3.0)"
    exit 1
fi

# Check for uncommitted changes
if ! git diff --quiet || ! git diff --cached --quiet; then
    echo "Error: You have uncommitted changes. Commit or stash them first."
    exit 1
fi

# Make sure we're on main and up to date
git checkout main
git pull origin main

# Create release branch
BRANCH="release/v$VERSION"
echo "Creating branch $BRANCH..."
git checkout -b "$BRANCH"

# Update version in .csproj
sed -i '' "s|<Version>.*</Version>|<Version>$VERSION</Version>|" "$CSPROJ"

# Update CHANGELOG.md - add new section after header
DATE=$(date +%Y-%m-%d)
# Create temp file with new changelog content
{
    head -1 "$CHANGELOG"
    echo ""
    echo "## [$VERSION] - $DATE"
    echo ""
    echo "### Added"
    echo ""
    echo "### Changed"
    echo ""
    echo "### Fixed"
    echo ""
    tail -n +2 "$CHANGELOG"
} > "$CHANGELOG.tmp"
mv "$CHANGELOG.tmp" "$CHANGELOG"

echo "Updated .csproj and CHANGELOG.md"
echo ""
echo "Please edit CHANGELOG.md to add release notes, then press Enter to continue..."
read -p ""

# Commit and push
git add "$CSPROJ" "$CHANGELOG"
git commit -m "Prepare release v$VERSION"
git push -u origin "$BRANCH"

# Create PR using gh CLI
echo "Creating pull request..."
gh pr create \
    --title "Prepare release v$VERSION" \
    --body "## Release v$VERSION

- Updates version to $VERSION
- Updates CHANGELOG.md

After merging, create a GitHub release with tag \`v$VERSION\` to publish to NuGet." \
    --base main

echo ""
echo "Done! PR created."
echo ""
echo "Next steps:"
echo "1. Review and merge the PR"
echo "2. Create a GitHub release with tag v$VERSION"
echo "   https://github.com/shreshthkhilani/filmstruck/releases/new?tag=v$VERSION"
