#!/usr/bin/env bash
# Start, stop, restart or check the Flaky Shop practice app (apps/FlakyShop) that the Lab tests run against.
#   bash .claude/skills/dotnet-ui-test/scripts/flakyshop.sh status|start|stop|restart [port]
# The port defaults to 5070, the FlakyShopUrl default. Restarting clears the app's shared in-memory state.
set -uo pipefail

command="${1:-status}"
port="${2:-5070}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
url="http://localhost:${port}"
log="${root}/.playwright-cli/flakyshop-${port}.log"

is_up() {
  curl -s -o /dev/null --max-time 2 "${url}/"
}

listening_pids() {
  if command -v powershell.exe >/dev/null 2>&1; then
    powershell.exe -NoProfile -Command \
      "Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique" \
      | tr -d '\r'
  else
    lsof -ti "tcp:${port}" -sTCP:LISTEN 2>/dev/null
  fi
}

stop_app() {
  local pids
  pids="$(listening_pids)"
  if [ -z "${pids}" ]; then
    echo "Flaky Shop is not running on port ${port}."
    return 0
  fi
  for pid in ${pids}; do
    if command -v taskkill >/dev/null 2>&1; then
      taskkill //PID "${pid}" //T //F >/dev/null 2>&1
    else
      kill "${pid}" 2>/dev/null
    fi
  done
  for _ in 1 2 3 4 5 6 7 8 9 10; do
    if [ -z "$(listening_pids)" ]; then
      echo "Stopped Flaky Shop on port ${port} (pid $(echo ${pids}))."
      return 0
    fi
  done
  echo "Port ${port} is still in use after stopping pid $(echo ${pids})." >&2
  return 1
}

start_app() {
  if is_up; then
    echo "Flaky Shop is already running at ${url}."
    return 0
  fi
  mkdir -p "$(dirname "${log}")"
  (cd "${root}" && nohup dotnet run --project apps/FlakyShop --urls "http://0.0.0.0:${port}" >"${log}" 2>&1 &)
  if curl -s -o /dev/null --max-time 2 --retry 90 --retry-delay 1 --retry-connrefused --retry-all-errors "${url}/"; then
    echo "Flaky Shop is running at ${url} (log: ${log})."
  else
    echo "Flaky Shop did not answer at ${url} within about 90 s. Last log lines:" >&2
    tail -n 20 "${log}" >&2
    return 1
  fi
}

case "${command}" in
  status)
    if is_up; then
      echo "Flaky Shop is running at ${url}."
    else
      echo "Flaky Shop is not running at ${url}."
      exit 1
    fi
    ;;
  start) start_app ;;
  stop) stop_app ;;
  restart) stop_app && start_app ;;
  *)
    echo "usage: $0 status|start|stop|restart [port]" >&2
    exit 2
    ;;
esac
