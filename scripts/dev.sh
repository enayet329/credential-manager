#!/usr/bin/env bash
# CredVault dev helper.
#
# Subcommands:
#   up              Start SQL (docker compose), run backend API and Next.js frontend together.
#                   Ctrl+C cleanly stops both. Logs are prefixed [api] / [web].
#   up-remote       Same as 'up' but no Docker — the API reads appsettings.Production.json
#                   (so it points at your remote DB, e.g. Somee). Skips local migrations.
#   test            Run all backend tests + frontend lint/build.
#   reset-db        Wipe the local SQL Server volume and re-run migrations on next 'up'.
#   master-key      Print a fresh 32-byte master key (Base64) using the API's helper.
#   migrate         Run EF Core migrations against the local SQL Server.
#   help            Print this message.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_API="$REPO_ROOT/backend/src/CredVault.Api/CredVault.Api.csproj"
INFRA_PROJ="$REPO_ROOT/backend/src/CredVault.Infrastructure/CredVault.Infrastructure.csproj"
FRONTEND_DIR="$REPO_ROOT/frontend"
COMPOSE_FILE="$REPO_ROOT/docker-compose.yml"

CONNECTION_STRING="${CREDVAULT_CONNECTION_STRING:-Server=localhost,1433;Database=CredVault;User Id=sa;Password=Local_Dev_Passw0rd!;TrustServerCertificate=True;Encrypt=False;}"
API_PORT="${CREDVAULT_API_PORT:-8080}"
WEB_PORT="${CREDVAULT_WEB_PORT:-3000}"

color()    { printf "\033[%sm%s\033[0m" "$1" "$2"; }
log()      { printf "[%s] %s\n" "$(color "$1" "$2")" "$3"; }
log_api()  { log "36" "api" "$1"; }
log_web()  { log "35" "web" "$1"; }
log_sql()  { log "33" "sql" "$1"; }
log_run()  { log "32" "run" "$1"; }
log_fail() { log "31" "err" "$1"; }

need_cmd() {
  command -v "$1" >/dev/null 2>&1 || { log_fail "missing dependency: $1"; exit 1; }
}

ensure_sqlserver() {
  need_cmd docker
  log_sql "ensuring sqlserver container is up (docker compose up -d sqlserver)..."
  docker compose -f "$COMPOSE_FILE" up -d sqlserver
  # Wait for the SQL Server health check to flip to healthy.
  local attempts=0
  while [ "$attempts" -lt 60 ]; do
    state="$(docker inspect -f '{{.State.Health.Status}}' credvault-sqlserver 2>/dev/null || echo "starting")"
    if [ "$state" = "healthy" ]; then
      log_sql "ready."
      return 0
    fi
    sleep 1
    attempts=$((attempts + 1))
  done
  log_fail "sqlserver did not become healthy in 60s; check 'docker compose logs sqlserver'."
  exit 1
}

run_migrations() {
  need_cmd dotnet
  log_run "applying EF Core migrations..."
  CREDVAULT_DESIGNTIME_CONNECTION="$CONNECTION_STRING" \
    dotnet ef database update \
      --project "$INFRA_PROJ" \
      --startup-project "$INFRA_PROJ"
}

cmd_up() {
  ensure_sqlserver
  run_migrations

  : "${ConnectionStrings__DefaultConnection:=$CONNECTION_STRING}"
  : "${Jwt__Secret:=local-dev-only-secret-please-replace-32bytes}"
  : "${Jwt__Issuer:=credvault-local}"
  : "${Jwt__Audience:=credvault-local}"
  : "${MasterKey__Versions__0__Version:=1}"
  : "${MasterKey__Versions__0__Base64Key:=$(dotnet run --project "$BACKEND_API" -- generate-master-key 2>/dev/null | tail -1)}"

  export ConnectionStrings__DefaultConnection Jwt__Secret Jwt__Issuer Jwt__Audience
  export MasterKey__Versions__0__Version MasterKey__Versions__0__Base64Key
  export ASPNETCORE_URLS="http://localhost:$API_PORT"

  local api_pid=0 web_pid=0
  cleanup() {
    log_run "stopping..."
    [ "$api_pid" -ne 0 ] && kill "$api_pid" 2>/dev/null || true
    [ "$web_pid" -ne 0 ] && kill "$web_pid" 2>/dev/null || true
    wait 2>/dev/null || true
    exit 0
  }
  trap cleanup INT TERM

  log_api "starting backend on http://localhost:$API_PORT..."
  (dotnet run --project "$BACKEND_API" --no-launch-profile 2>&1 | sed -u 's/^/[\x1b[36mapi\x1b[0m] /') &
  api_pid=$!

  log_web "starting frontend on http://localhost:$WEB_PORT..."
  (cd "$FRONTEND_DIR" && NEXT_PUBLIC_API_URL="http://localhost:$API_PORT" PORT="$WEB_PORT" npm run dev 2>&1 | sed -u 's/^/[\x1b[35mweb\x1b[0m] /') &
  web_pid=$!

  log_run "Ctrl+C stops both. api=$api_pid web=$web_pid"
  wait
}

cmd_up_remote() {
  need_cmd dotnet
  need_cmd npm

  local prod_settings="$REPO_ROOT/backend/src/CredVault.Api/appsettings.Production.json"
  if [ ! -f "$prod_settings" ]; then
    log_fail "missing $prod_settings — without it the API has no connection string, JWT secret, or master key."
    exit 1
  fi

  export ASPNETCORE_ENVIRONMENT=Production
  export ASPNETCORE_URLS="http://localhost:$API_PORT"

  local api_pid=0 web_pid=0
  cleanup() {
    log_run "stopping..."
    [ "$api_pid" -ne 0 ] && kill "$api_pid" 2>/dev/null || true
    [ "$web_pid" -ne 0 ] && kill "$web_pid" 2>/dev/null || true
    wait 2>/dev/null || true
    exit 0
  }
  trap cleanup INT TERM

  log_run "ASPNETCORE_ENVIRONMENT=Production (reading appsettings.Production.json — remote DB)"
  log_api "starting backend on http://localhost:$API_PORT..."
  (dotnet run --project "$BACKEND_API" --no-launch-profile 2>&1 | sed -u 's/^/[\x1b[36mapi\x1b[0m] /') &
  api_pid=$!

  log_web "starting frontend on http://localhost:$WEB_PORT..."
  (cd "$FRONTEND_DIR" && NEXT_PUBLIC_API_URL="http://localhost:$API_PORT" PORT="$WEB_PORT" npm run dev 2>&1 | sed -u 's/^/[\x1b[35mweb\x1b[0m] /') &
  web_pid=$!

  log_run "Ctrl+C stops both. api=$api_pid web=$web_pid"
  wait
}

cmd_test() {
  need_cmd dotnet
  log_run "backend: dotnet test (Release)..."
  (cd "$REPO_ROOT/backend" && dotnet test CredVault.slnx --configuration Release --nologo)

  if [ -d "$FRONTEND_DIR" ]; then
    need_cmd npm
    log_run "frontend: npm install (silent)..."
    (cd "$FRONTEND_DIR" && npm install --silent --no-audit --no-fund >/dev/null)
    log_run "frontend: npm run lint..."
    (cd "$FRONTEND_DIR" && npm run lint --silent || true)
    log_run "frontend: npm run build..."
    (cd "$FRONTEND_DIR" && npm run build --silent)
  fi

  log_run "all green."
}

cmd_reset_db() {
  need_cmd docker
  log_sql "destroying sqlserver volume (data will be lost)..."
  docker compose -f "$COMPOSE_FILE" down -v sqlserver 2>/dev/null || \
    docker compose -f "$COMPOSE_FILE" rm -fs sqlserver
  docker volume rm unigate_sqlserver-data 2>/dev/null || true
  log_run "done. Run 'scripts/dev.sh up' to recreate."
}

cmd_master_key() {
  need_cmd dotnet
  dotnet run --project "$BACKEND_API" -- generate-master-key
}

cmd_migrate() {
  ensure_sqlserver
  run_migrations
}

usage() {
  sed -n '1,12p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

main() {
  local cmd="${1:-help}"
  shift || true
  case "$cmd" in
    up)         cmd_up "$@" ;;
    up-remote)  cmd_up_remote "$@" ;;
    test)       cmd_test "$@" ;;
    reset-db)   cmd_reset_db "$@" ;;
    master-key) cmd_master_key "$@" ;;
    migrate)    cmd_migrate "$@" ;;
    help|-h|--help) usage ;;
    *)
      log_fail "unknown subcommand: $cmd"
      usage
      exit 2
      ;;
  esac
}

main "$@"
