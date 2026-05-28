#!/usr/bin/env bash
# Apply migrations + run the idempotent initial-data seed against the DB in
# appsettings.Production.json. Safe to re-run; the seed no-ops if an org exists.
#
# Optional flags forwarded verbatim:
#   --org-name "..."        --org-slug "..."
#   --admin-email "..."     --admin-password "..."
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet
require_prod_settings

run "applying migrations + seeding initial data..."
dotnet run --project "$BACKEND_API" --no-launch-profile -- seed-initial-data "$@"
