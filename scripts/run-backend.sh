#!/usr/bin/env bash
# Start just the backend API against the remote DB. Ctrl+C stops it.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet
require_prod_settings

export ASPNETCORE_URLS="http://localhost:$API_PORT"

run "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT (reading appsettings.Production.json)"
api "starting backend on http://localhost:$API_PORT..."
dotnet run --project "$BACKEND_API" --no-launch-profile
