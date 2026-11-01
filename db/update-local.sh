#!/usr/bin/env bash
# Brings the local database (compose's throughline-pg) up to date with db/changelog: creates any
# missing schema, then applies each module's pending changesets.
#
# Usage: db/update-local.sh [liquibase-command]
#   No argument runs update. Pass update-sql to preview, or status to list what's pending.
set -euo pipefail

cd "$(dirname "$0")/.."

command="${1:-update}"
modules="orders inventory receiving wolverine"

# Cached after the first build, so this is quick unless db/Dockerfile changed
docker build -q -t throughline-liquibase db/ >/dev/null

docker exec -i -e PGOPTIONS="-c client_min_messages=warning" throughline-pg \
  psql -U postgres -d throughline -q -v ON_ERROR_STOP=1 < db/create-schemas.sql

for module in $modules; do
  echo "== $module"
  docker run --rm --network container:throughline-pg \
    -v "$PWD/db/changelog:/liquibase/changelog" throughline-liquibase \
    --url=jdbc:postgresql://localhost:5432/throughline --username=postgres --password=postgres \
    --show-banner=false --changelog-file="$module/changelog.yaml" --liquibase-schema-name="$module" \
    "$command"
done
