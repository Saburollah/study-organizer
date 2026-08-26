#!/bin/sh

set -eu

echo "Applying pending database migrations before API startup."
if ! /app/efbundle --no-color; then
  echo "Database migration failed. API startup aborted." >&2
  exit 1
fi

echo "Database migrations completed. Starting API."
exec dotnet /app/StudyOrganizer.Api.dll
