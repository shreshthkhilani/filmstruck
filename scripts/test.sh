#!/bin/bash
# Build CLI and run comprehensive tests against a dynamically generated test site

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$SCRIPT_DIR/.."
PROJECT="$REPO_ROOT/src/FilmStruck.Cli/FilmStruck.Cli.csproj"
TEST_SITE="$REPO_ROOT/test/sample-site"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Test counters
PASSED=0
FAILED=0
SKIPPED=0
FAILED_TESTS=""

pass() {
    PASSED=$((PASSED + 1))
}

fail() {
    FAILED=$((FAILED + 1))
    FAILED_TESTS="$FAILED_TESTS\n  - $1"
}

skip() {
    SKIPPED=$((SKIPPED + 1))
}

run_command() {
    dotnet run --project "$PROJECT" -v quiet -- "$@" 2>&1
}

setup_test_site() {
    rm -rf "$TEST_SITE"
    mkdir -p "$TEST_SITE/data"

    # Initialize git repo (required by CsvService)
    cd "$TEST_SITE"
    git init -q
    git config user.email "test@example.com"
    git config user.name "Test"

    # Create config
    cat > filmstruck.json << 'EOF'
{
  "username": "testuser",
  "siteTitle": "Test Film Log"
}
EOF

    # Create log.csv with sample films (tmdbId known for testing)
    cat > data/log.csv << 'EOF'
date,title,location,companions,tmdbId
1/15/2024,The Godfather,Home,,238
1/20/2024,Pulp Fiction,Theater,Alice,680
2/1/2024,Inception,Home,Bob,27205
EOF
}

cleanup() {
    rm -rf "$TEST_SITE" 2>/dev/null || true
}

print_summary() {
    echo ""
    echo -e "${BOLD}Test Results${NC}"
    echo "─────────────────────────────"

    TOTAL=$((PASSED + FAILED + SKIPPED))

    if [ $PASSED -gt 0 ]; then
        echo -e "${GREEN}✓ $PASSED passed${NC}"
    fi

    if [ $SKIPPED -gt 0 ]; then
        echo -e "${YELLOW}○ $SKIPPED skipped${NC}"
    fi

    if [ $FAILED -gt 0 ]; then
        echo -e "${RED}✗ $FAILED failed${NC}"
        echo -e "${RED}Failed tests:$FAILED_TESTS${NC}"
    fi

    echo ""

    if [ $FAILED -gt 0 ]; then
        exit 1
    fi
}

# Trap to ensure cleanup on exit
trap cleanup EXIT

# Build quietly
dotnet build "$PROJECT" -v quiet

setup_test_site
cd "$TEST_SITE"

# Test 1: enrich command (creates films.csv from log.csv)
if [ -n "$TMDB_API_KEY" ]; then
    rm -f data/films.csv
    OUTPUT=$(run_command enrich --default-poster 2>&1) || true
    if [ -f "data/films.csv" ] && [ $(wc -l < data/films.csv) -gt 1 ]; then
        pass "enrich"
    else
        fail "enrich: films.csv not created or empty"
    fi
else
    skip "enrich"
    # Create films.csv manually for subsequent tests
    cat > data/films.csv << 'EOF'
tmdbId,title,director,releaseYear,language,posterPath
238,The Godfather,Francis Ford Coppola,1972,en,/3bhkrj58Vtu7enYsRolD1fZdja1.jpg
680,Pulp Fiction,Quentin Tarantino,1994,en,/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg
27205,Inception,Christopher Nolan,2010,en,/oYuLEt3zVCKq57qu2F8dT7NIa6f.jpg
EOF
fi

# Test 2: calculate command (creates stats.csv)
rm -f data/stats.csv
OUTPUT=$(run_command calculate 2>&1) || true
if [ -f "data/stats.csv" ] && [ $(wc -l < data/stats.csv) -gt 1 ]; then
    pass "calculate"
else
    fail "calculate: stats.csv not created or empty"
fi

# Test 3: add command (requires TMDB_API_KEY)
if [ -n "$TMDB_API_KEY" ]; then
    LOG_LINES_BEFORE=$(wc -l < data/log.csv)
    OUTPUT=$(run_command add --title "The Matrix" --tmdb-id 603 --date "3/1/2024" --location "Home" --companions "" --default-poster 2>&1) || true
    LOG_LINES_AFTER=$(wc -l < data/log.csv)

    if [ "$LOG_LINES_AFTER" -gt "$LOG_LINES_BEFORE" ]; then
        pass "add: log.csv updated"
    else
        fail "add: log.csv not updated"
    fi

    # Verify films.csv has The Matrix (tmdbId 603)
    if grep -q "603" data/films.csv; then
        pass "add: films.csv updated"
    else
        fail "add: films.csv not updated"
    fi
else
    skip "add"
fi

# Test 4: build command (creates index.html)
rm -f index.html
OUTPUT=$(run_command build 2>&1) || true
if [ -f "index.html" ]; then
    pass "build"
else
    fail "build: index.html not generated"
fi

# Test 5: hearts commands (add and remove favorites)
rm -f data/hearts.csv
run_command hearts add --tmdb-id 238 >/dev/null 2>&1 || true
run_command hearts add --tmdb-id 680 >/dev/null 2>&1 || true
run_command hearts remove --tmdb-id 238 >/dev/null 2>&1 || true
if [ -f "data/hearts.csv" ] && grep -q "680" data/hearts.csv && ! grep -q "238" data/hearts.csv; then
    pass "hearts"
else
    fail "hearts: expected 680 in hearts.csv and 238 removed"
fi

print_summary
