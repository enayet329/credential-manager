#!/usr/bin/env bash
# Start the backend API and the Next.js frontend together, reading
# appsettings.Production.json (so the API talks to your remote SQL Server).
# No Docker. Ctrl+C stops both.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet
need_cmd npm
require_prod_settings

export ASPNETCORE_URLS="http://localhost:$API_PORT"

api_pid=0
web_pid=0
cleanup() {
  run "stopping..."
  [ "$api_pid" -ne 0 ] && kill "$api_pid" 2>/dev/null || true
  [ "$web_pid" -ne 0 ] && kill "$web_pid" 2>/dev/null || true
  wait 2>/dev/null || true
  exit 0
}
trap cleanup INT TERM

run "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT (reading appsettings.Production.json)"
api "starting backend on http://localhost:$API_PORT..."
(dotnet run --project "$BACKEND_API" --no-launch-profile 2>&1 \
  | sed -u 's/^/[\x1b[36mapi\x1b[0m] /') &
api_pid=$!

web "starting frontend on http://localhost:$WEB_PORT..."
(cd "$FRONTEND_DIR" && NEXT_PUBLIC_API_URL="http://localhost:$API_PORT" PORT="$WEB_PORT" \
  npm run dev 2>&1 | sed -u 's/^/[\x1b[35mweb\x1b[0m] /') &
web_pid=$!

run "Ctrl+C stops both. api=$api_pid web=$web_pid"
wait
