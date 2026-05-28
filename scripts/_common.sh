#!/usr/bin/env bash
# Shared helpers sourced by the individual scripts in this folder.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_API="$REPO_ROOT/backend/src/CredVault.Api/CredVault.Api.csproj"
FRONTEND_DIR="$REPO_ROOT/frontend"
PROD_SETTINGS="$REPO_ROOT/backend/src/CredVault.Api/appsettings.Production.json"

API_PORT="${CREDVAULT_API_PORT:-8080}"
WEB_PORT="${CREDVAULT_WEB_PORT:-3000}"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"

color() { printf "\033[%sm%s\033[0m" "$1" "$2"; }
log()   { printf "[%s] %s\n" "$(color "$1" "$2")" "$3"; }
api()   { log "36" "api" "$1"; }
web()   { log "35" "web" "$1"; }
run()   { log "32" "run" "$1"; }
err()   { log "31" "err" "$1"; }

need_cmd()  { command -v "$1" >/dev/null 2>&1 || { err "missing dependency: $1"; exit 1; }; }

require_prod_settings() {
  if [ ! -f "$PROD_SETTINGS" ]; then
    err "missing $PROD_SETTINGS"
    err "without it the API has no connection string, JWT secret, or master key."
    exit 1
  fi
}
