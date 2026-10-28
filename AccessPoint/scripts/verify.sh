#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build --nologo
for file in wwwroot/*.js; do node --check "$file"; done
verification_dir=$(mktemp -d)
trap 'if [ -n "${api_pid:-}" ]; then kill "$api_pid" 2>/dev/null || true; fi; rm -rf "$verification_dir"' EXIT
export Seed__Password="$(python3 -c 'import secrets; print("Verify!9"+secrets.token_hex(12))')"
export TEST_PASSWORD="$Seed__Password"
export ConnectionStrings__App="Data Source=$verification_dir/test.db"
export Database__Provider=Sqlite
export ASPNETCORE_ENVIRONMENT=Development
export APP_BASE="http://127.0.0.1:${TEST_PORT:-5080}"
dotnet run --no-build --no-launch-profile --urls "$APP_BASE" > "$verification_dir/server.log" 2>&1 &
api_pid=$!
if ! python3 tests/api_regression.py; then cat "$verification_dir/server.log"; exit 1; fi
