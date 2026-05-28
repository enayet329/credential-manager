#!/usr/bin/env bash
# Apply EF Core migrations to the database in appsettings.Production.json.
# Idempotent — already-applied migrations are skipped.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet
require_prod_settings

run "applying migrations against the DB in appsettings.Production.json..."
dotnet run --project "$BACKEND_API" --no-launch-profile -- migrate
