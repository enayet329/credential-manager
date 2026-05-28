#!/usr/bin/env bash
# Print a fresh 32-byte master key (Base64). Paste into appsettings.Production.json.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet
# Don't load Production config for this — it's a pure helper.
unset ASPNETCORE_ENVIRONMENT

dotnet run --project "$BACKEND_API" --no-launch-profile -- generate-master-key
