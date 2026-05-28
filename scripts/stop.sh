#!/usr/bin/env bash
# Kill anything listening on the API and web ports. Safe to run repeatedly.
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

kill_port() {
  local port="$1" name="$2"
  local pids
  pids=$(lsof -ti "tcp:$port" 2>/dev/null || true)
  if [ -n "$pids" ]; then
    run "stopping $name (port $port, PIDs: $pids)"
    kill $pids 2>/dev/null || true
    sleep 1
    pids=$(lsof -ti "tcp:$port" 2>/dev/null || true)
    if [ -n "$pids" ]; then
      run "forcing kill -9 on $name (PIDs: $pids)"
      kill -9 $pids 2>/dev/null || true
    fi
  else
    run "nothing on port $port ($name)"
  fi
}

kill_port "$API_PORT" "backend"
kill_port "$WEB_PORT" "frontend"
run "done."
