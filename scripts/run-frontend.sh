#!/usr/bin/env bash
# Start just the Next.js frontend. Ctrl+C stops it.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

need_cmd npm

web "starting frontend on http://localhost:$WEB_PORT..."
cd "$FRONTEND_DIR"
NEXT_PUBLIC_API_URL="http://localhost:$API_PORT" PORT="$WEB_PORT" npm run dev
