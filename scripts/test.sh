#!/usr/bin/env bash
# Run the full backend test suite (Release) and the frontend lint+build.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd dotnet

# Tests should NOT touch the production DB — drop the ASPNETCORE_ENVIRONMENT default
# from _common.sh so the test fixtures pick their own configuration.
unset ASPNETCORE_ENVIRONMENT

run "backend tests (Release)..."
(cd "$REPO_ROOT/backend" && dotnet test CredVault.slnx --configuration Release --nologo)

if [ -d "$FRONTEND_DIR" ]; then
  need_cmd npm
  run "frontend deps..."
  (cd "$FRONTEND_DIR" && npm install --silent --no-audit --no-fund >/dev/null)
  run "frontend lint..."
  (cd "$FRONTEND_DIR" && npm run lint --silent || true)
  run "frontend build..."
  (cd "$FRONTEND_DIR" && npm run build --silent)
fi

run "all green."
