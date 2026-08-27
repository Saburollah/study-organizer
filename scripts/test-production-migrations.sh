#!/usr/bin/env bash

set -euo pipefail

DEPLOYMENT_TEST_SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DEPLOYMENT_TEST_REPOSITORY_ROOT="$(cd -- "$DEPLOYMENT_TEST_SCRIPT_DIR/.." && pwd)"
DEPLOYMENT_TEST_SUFFIX="$$"
DEPLOYMENT_TEST_IMAGE="study-organizer-deployment-test:$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_NETWORK="study-organizer-deployment-test-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_POSTGRES="study-organizer-deployment-postgres-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_EMPTY_API="study-organizer-deployment-api-empty-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_UPGRADE_API="study-organizer-deployment-api-upgrade-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_REPEAT_API="study-organizer-deployment-api-repeat-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_FAILURE_API="study-organizer-deployment-api-failure-$DEPLOYMENT_TEST_SUFFIX"
DEPLOYMENT_TEST_EMPTY_DATABASE="empty_migration_case"
DEPLOYMENT_TEST_UPGRADE_DATABASE="upgrade_migration_case"
DEPLOYMENT_TEST_FAILURE_DATABASE="failure_migration_case"
DEPLOYMENT_TEST_USER="deployment_test"
DEPLOYMENT_TEST_PASSWORD="deployment-test-password"
DEPLOYMENT_TEST_PREVIOUS_MIGRATION="20260813081919_AddUserProfile"
DEPLOYMENT_TEST_CURRENT_MIGRATION="20260826164845_AddExternalCourseCleanup"

cleanup_deployment_test() {
  docker rm --force "$DEPLOYMENT_TEST_EMPTY_API" >/dev/null 2>&1 || true
  docker rm --force "$DEPLOYMENT_TEST_UPGRADE_API" >/dev/null 2>&1 || true
  docker rm --force "$DEPLOYMENT_TEST_REPEAT_API" >/dev/null 2>&1 || true
  docker rm --force "$DEPLOYMENT_TEST_FAILURE_API" >/dev/null 2>&1 || true
  docker rm --force "$DEPLOYMENT_TEST_POSTGRES" >/dev/null 2>&1 || true
  docker network rm "$DEPLOYMENT_TEST_NETWORK" >/dev/null 2>&1 || true
  docker image rm "$DEPLOYMENT_TEST_IMAGE" >/dev/null 2>&1 || true
}

fail_deployment_test() {
  echo "FEHLER: $1" >&2
  exit 1
}

wait_for_postgres() {
  for _ in {1..60}; do
    if docker exec "$DEPLOYMENT_TEST_POSTGRES" \
      pg_isready --username "$DEPLOYMENT_TEST_USER" --dbname postgres \
      >/dev/null 2>&1; then
      return
    fi

    if [[ "$(docker inspect --format '{{.State.Running}}' "$DEPLOYMENT_TEST_POSTGRES")" != "true" ]]; then
      docker logs "$DEPLOYMENT_TEST_POSTGRES" >&2
      fail_deployment_test "PostgreSQL wurde vorzeitig beendet."
    fi

    sleep 1
  done

  fail_deployment_test "PostgreSQL wurde nicht rechtzeitig bereit."
}

wait_for_api() {
  local api_container="$1"
  local api_port
  api_port="$(docker port "$api_container" 10000/tcp | sed 's/.*://')"

  for _ in {1..60}; do
    if curl --fail --silent "http://127.0.0.1:$api_port/health" >/dev/null; then
      return
    fi

    if [[ "$(docker inspect --format '{{.State.Running}}' "$api_container")" != "true" ]]; then
      docker logs "$api_container" >&2
      fail_deployment_test "Die API wurde vor dem Health Check beendet."
    fi

    sleep 1
  done

  docker logs "$api_container" >&2
  fail_deployment_test "Die API wurde nicht rechtzeitig bereit."
}

trap cleanup_deployment_test EXIT

cd "$DEPLOYMENT_TEST_REPOSITORY_ROOT"

echo "Baue das produktive Runtime-Image …"
docker build --tag "$DEPLOYMENT_TEST_IMAGE" .

docker network create "$DEPLOYMENT_TEST_NETWORK" >/dev/null

docker run --detach \
  --name "$DEPLOYMENT_TEST_POSTGRES" \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --network-alias postgres \
  --env "POSTGRES_DB=postgres" \
  --env "POSTGRES_USER=$DEPLOYMENT_TEST_USER" \
  --env "POSTGRES_PASSWORD=$DEPLOYMENT_TEST_PASSWORD" \
  postgres:16-alpine >/dev/null

wait_for_postgres

docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  createdb --username "$DEPLOYMENT_TEST_USER" "$DEPLOYMENT_TEST_EMPTY_DATABASE"

docker run --detach \
  --name "$DEPLOYMENT_TEST_EMPTY_API" \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --publish 127.0.0.1::10000 \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_EMPTY_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" >/dev/null

wait_for_api "$DEPLOYMENT_TEST_EMPTY_API"

schema_is_current="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_EMPTY_DATABASE" \
    --tuples-only --no-align \
    --command "SELECT to_regclass('public.course_subscriptions') IS NOT NULL AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'external_learning_contents' AND column_name = 'metadata_purged_at');")"

if [[ "$schema_is_current" != "t" ]]; then
  fail_deployment_test \
    "Die API wurde bereitgestellt, bevor die ausstehenden Migrationen angewendet waren."
fi

echo "OK: Eine leere Datenbank wird vor dem API-Start vollständig migriert."

runtime_sdks="$(docker run --rm --entrypoint dotnet "$DEPLOYMENT_TEST_IMAGE" --list-sdks)"
if [[ -n "$runtime_sdks" ]]; then
  fail_deployment_test "Das finale Runtime-Image enthält ein .NET SDK."
fi

if ! docker run --rm --entrypoint sh "$DEPLOYMENT_TEST_IMAGE" \
  -c 'test -x /app/efbundle && ! command -v dotnet-ef'; then
  fail_deployment_test \
    "Das Runtime-Image enthält kein ausführbares Bundle oder ein globales dotnet-ef."
fi

echo "OK: Das Runtime-Image benötigt weder SDK noch globales dotnet-ef."

docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  createdb --username "$DEPLOYMENT_TEST_USER" "$DEPLOYMENT_TEST_UPGRADE_DATABASE"

if ! docker run --rm \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --entrypoint /app/efbundle \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_UPGRADE_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" \
  "$DEPLOYMENT_TEST_PREVIOUS_MIGRATION" --no-color; then
  fail_deployment_test \
    "Der vorherige Migrationsstand konnte für den Upgrade-Test nicht eingerichtet werden."
fi

docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
  --set ON_ERROR_STOP=1 \
  --command \
  "INSERT INTO \"AspNetUsers\" (\"Id\", \"Email\", \"NormalizedEmail\", \"EmailConfirmed\", \"PhoneNumberConfirmed\", \"TwoFactorEnabled\", \"LockoutEnabled\", \"AccessFailedCount\") VALUES ('00000000-0000-0000-0000-000000000090', 'preserved@example.test', 'PRESERVED@EXAMPLE.TEST', false, false, false, true, 0); INSERT INTO modules (id, owner_id, name, created_at) VALUES ('00000000-0000-0000-0000-000000000091', '00000000-0000-0000-0000-000000000090', 'Preserved module', now());" \
  >/dev/null

docker run --detach \
  --name "$DEPLOYMENT_TEST_UPGRADE_API" \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --publish 127.0.0.1::10000 \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_UPGRADE_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" >/dev/null

wait_for_api "$DEPLOYMENT_TEST_UPGRADE_API"

current_migration_count="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
    --tuples-only --no-align \
    --command "SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '$DEPLOYMENT_TEST_CURRENT_MIGRATION';")"
preserved_module_count="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
    --tuples-only --no-align \
    --command "SELECT count(*) FROM modules WHERE name = 'Preserved module';")"
migration_count_before_repeat="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
    --tuples-only --no-align \
    --command 'SELECT count(*) FROM "__EFMigrationsHistory";')"

if [[ "$current_migration_count" != "1" || "$preserved_module_count" != "1" ]]; then
  fail_deployment_test \
    "Das Upgrade hat die aktuelle Migration oder vorhandene Daten nicht erhalten."
fi

docker rm --force "$DEPLOYMENT_TEST_UPGRADE_API" >/dev/null

docker run --detach \
  --name "$DEPLOYMENT_TEST_REPEAT_API" \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --publish 127.0.0.1::10000 \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_UPGRADE_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" >/dev/null

wait_for_api "$DEPLOYMENT_TEST_REPEAT_API"

migration_count_after_repeat="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
    --tuples-only --no-align \
    --command 'SELECT count(*) FROM "__EFMigrationsHistory";')"
preserved_module_count="$(docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_UPGRADE_DATABASE" \
    --tuples-only --no-align \
    --command "SELECT count(*) FROM modules WHERE name = 'Preserved module';")"

if [[ "$migration_count_after_repeat" != "$migration_count_before_repeat" \
  || "$preserved_module_count" != "1" ]]; then
  fail_deployment_test \
    "Der wiederholte Start war nicht idempotent oder hat vorhandene Daten verändert."
fi

echo "OK: Upgrade und wiederholter Start sind datenerhaltend und idempotent."

docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  createdb --username "$DEPLOYMENT_TEST_USER" "$DEPLOYMENT_TEST_FAILURE_DATABASE"

if ! docker run --rm \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --entrypoint /app/efbundle \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_FAILURE_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" \
  "$DEPLOYMENT_TEST_PREVIOUS_MIGRATION" --no-color >/dev/null; then
  fail_deployment_test \
    "Der vorherige Migrationsstand konnte für den Fehlertest nicht eingerichtet werden."
fi

docker exec "$DEPLOYMENT_TEST_POSTGRES" \
  psql --username "$DEPLOYMENT_TEST_USER" --dbname "$DEPLOYMENT_TEST_FAILURE_DATABASE" \
  --set ON_ERROR_STOP=1 \
  --command 'CREATE TABLE external_courses (collision_marker integer);' \
  >/dev/null

docker run --detach \
  --name "$DEPLOYMENT_TEST_FAILURE_API" \
  --network "$DEPLOYMENT_TEST_NETWORK" \
  --env "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=$DEPLOYMENT_TEST_FAILURE_DATABASE;Username=$DEPLOYMENT_TEST_USER;Password=$DEPLOYMENT_TEST_PASSWORD" \
  --env "Jwt__SigningKey=deployment-test-signing-key-with-at-least-32-characters" \
  "$DEPLOYMENT_TEST_IMAGE" >/dev/null

for _ in {1..60}; do
  if [[ "$(docker inspect --format '{{.State.Running}}' "$DEPLOYMENT_TEST_FAILURE_API")" != "true" ]]; then
    break
  fi

  sleep 1
done

if [[ "$(docker inspect --format '{{.State.Running}}' "$DEPLOYMENT_TEST_FAILURE_API")" == "true" ]]; then
  fail_deployment_test "Die API startete trotz fehlgeschlagener Migration."
fi

failure_exit_code="$(docker inspect --format '{{.State.ExitCode}}' "$DEPLOYMENT_TEST_FAILURE_API")"
failure_logs="$(docker logs "$DEPLOYMENT_TEST_FAILURE_API" 2>&1)"

if [[ "$failure_exit_code" == "0" ]]; then
  fail_deployment_test "Der Migrationsfehler lieferte keinen Fehlerstatus."
fi

if [[ "$failure_logs" == *"$DEPLOYMENT_TEST_PASSWORD"* ]]; then
  fail_deployment_test "Die Fehlermeldung enthält das Datenbankpasswort."
fi

if [[ "$failure_logs" != *"Database migration failed. API startup aborted."* ]]; then
  redacted_failure_logs="${failure_logs//$DEPLOYMENT_TEST_PASSWORD/<REDACTED>}"
  echo "$redacted_failure_logs" >&2
  fail_deployment_test "Der Migrationsfehler erklärt den abgebrochenen API-Start nicht klar."
fi

echo "OK: Ein Migrationsfehler beendet das Deployment klar und ohne Geheimnisse."
