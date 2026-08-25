#!/usr/bin/env bash

set -euo pipefail

E2E_SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
E2E_REPOSITORY_ROOT="$(cd -- "$E2E_SCRIPT_DIR/.." && pwd)"
E2E_ENV_FILE="$E2E_REPOSITORY_ROOT/.env"

if [[ ! -f "$E2E_ENV_FILE" ]]; then
  echo "Fehlende .env-Datei. Kopiere zuerst .env.example nach .env." >&2
  exit 1
fi

set -a
source "$E2E_ENV_FILE"
set +a

: "${POSTGRES_DB:?POSTGRES_DB fehlt in .env}"
: "${POSTGRES_USER:?POSTGRES_USER fehlt in .env}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD fehlt in .env}"

E2E_DATABASE_NAME="${POSTGRES_DB}_e2e"
E2E_DATABASE_PORT="${POSTGRES_PORT:-5432}"

if [[ ! "$E2E_DATABASE_NAME" =~ ^[a-zA-Z0-9_]+$ ]] || (( ${#E2E_DATABASE_NAME} > 63 )); then
  echo "Der abgeleitete E2E-Datenbankname ist ungültig." >&2
  exit 1
fi

cd "$E2E_REPOSITORY_ROOT"

E2E_POSTGRES_WAS_RUNNING="$(docker compose ps --status running --quiet postgres)"

drop_e2e_database() {
  docker compose exec -T postgres \
    dropdb --if-exists --force --username "$POSTGRES_USER" \
      --maintenance-db "$POSTGRES_DB" "$E2E_DATABASE_NAME" \
    >/dev/null 2>&1 || true
}

cleanup_e2e_environment() {
  drop_e2e_database
  if [[ -z "$E2E_POSTGRES_WAS_RUNNING" ]]; then
    docker compose stop postgres >/dev/null 2>&1 || true
  fi
}

trap cleanup_e2e_environment EXIT

echo "Starte PostgreSQL und bereite die isolierte E2E-Datenbank vor …"
docker compose up -d --wait postgres
drop_e2e_database
docker compose exec -T postgres \
  createdb --username "$POSTGRES_USER" \
    --maintenance-db "$POSTGRES_DB" "$E2E_DATABASE_NAME"

export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=$E2E_DATABASE_PORT;Database=$E2E_DATABASE_NAME;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
export Jwt__SigningKey="playwright-only-signing-key-with-at-least-32-characters"

dotnet tool restore
env "Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning" \
  dotnet tool run dotnet-ef database update \
  --project backend/src/Infrastructure \
  --startup-project backend/src/Api

echo "Starte den Playwright-Golden-Path …"
cd "$E2E_REPOSITORY_ROOT/frontend"
pnpm exec playwright test "$@"
