#!/usr/bin/env bash
set -euo pipefail

ENDPOINT="http://localhost:8000"
TABLE_NAME="filmstruck-staging"
REGION="us-east-1"

echo "Creating DynamoDB table: $TABLE_NAME"
aws dynamodb create-table \
  --table-name "$TABLE_NAME" \
  --attribute-definitions \
    AttributeName=PartitionKey,AttributeType=S \
    AttributeName=SortKey,AttributeType=S \
  --key-schema \
    AttributeName=PartitionKey,KeyType=HASH \
    AttributeName=SortKey,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT" \
  --region "$REGION" \
  2>/dev/null || echo "Table already exists, continuing..."

echo "Seeding sample data..."

USERNAME="testuser"

# Seed Film items
aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Film#278"},
  "tmdbId": {"S": "278"},
  "title": {"S": "The Shawshank Redemption"},
  "director": {"S": "Frank Darabont"},
  "releaseYear": {"S": "1994"},
  "language": {"S": "en"},
  "posterPath": {"S": "/9cjIGRiQoJmTnaE-HGYUhKZe4gf.jpg"}
}'

aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Film#238"},
  "tmdbId": {"S": "238"},
  "title": {"S": "The Godfather"},
  "director": {"S": "Francis Ford Coppola"},
  "releaseYear": {"S": "1972"},
  "language": {"S": "en"},
  "posterPath": {"S": "/3bhkrj58Vtu7enYsRolD1fZdja1.jpg"}
}'

aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Film#680"},
  "tmdbId": {"S": "680"},
  "title": {"S": "Pulp Fiction"},
  "director": {"S": "Quentin Tarantino"},
  "releaseYear": {"S": "1994"},
  "language": {"S": "en"},
  "posterPath": {"S": "/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg"}
}'

# Seed Log items
aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Log#2025-01-15#278"},
  "date": {"S": "1/15/2025"},
  "title": {"S": "The Shawshank Redemption"},
  "location": {"S": "Home"},
  "companions": {"S": ""},
  "tmdbId": {"S": "278"}
}'

aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Log#2025-02-01#238"},
  "date": {"S": "2/1/2025"},
  "title": {"S": "The Godfather"},
  "location": {"S": "Theater"},
  "companions": {"S": "Alice"},
  "tmdbId": {"S": "238"}
}'

aws dynamodb put-item --table-name "$TABLE_NAME" --endpoint-url "$ENDPOINT" --region "$REGION" --item '{
  "PartitionKey": {"S": "'"$USERNAME"'"},
  "SortKey": {"S": "Log#2025-02-10#680"},
  "date": {"S": "2/10/2025"},
  "title": {"S": "Pulp Fiction"},
  "location": {"S": "Home"},
  "companions": {"S": "Bob, Carol"},
  "tmdbId": {"S": "680"}
}'

echo "Done! Seeded 3 films and 3 log entries for user: $USERNAME"
echo "Test with: curl http://localhost:5000/api/$USERNAME"
