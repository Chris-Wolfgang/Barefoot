#!/usr/bin/env bash
# Applies Barefoot's migrations up, down and up again on a real SQL Server, and checks that the
# script from `bf-maint migrate --script` builds the same schema (ADR 0001 section 4,
# docs/DATABASE-CONVENTIONS.md, docs/MIGRATE.md).
#
# It starts a throwaway SQL Server container, runs the bf-maint that this checkout builds against
# it, and removes the container when it ends. The sa password is made here for this one run and
# is never printed. Needs: bash, docker, the .NET SDK. Run it from the repository root:
#
#     bash scripts/check-migration.sh
#
# Exit code 0 means every step did what docs/MIGRATE.md says. Any other code names the step.

set -euo pipefail

# Git Bash on Windows rewrites arguments that look like Unix paths; the paths below are for the
# container, so that is switched off. The variable means nothing on Linux.
export MSYS_NO_PATHCONV=1

IMAGE="${BAREFOOT_SQL_IMAGE:-mcr.microsoft.com/mssql/server:2022-latest}"
PORT="${BAREFOOT_SQL_PORT:-14330}"
NAME="barefoot-migration-check-$$"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
WORK="$(mktemp -d)"
if command -v cygpath >/dev/null 2>&1; then
  # Git Bash: give .NET and docker a path Windows understands.
  WORK="$(cygpath -m "$WORK")"
fi

password="Bf-$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')-aA1"
if [ "${GITHUB_ACTIONS:-}" = "true" ]; then
  echo "::add-mask::$password"
fi

cleanup() {
  docker rm -f "$NAME" >/dev/null 2>&1 || true
  rm -rf "$WORK"
}
trap cleanup EXIT

fail() {
  echo "FAILED: $*" >&2
  exit 1
}

# Runs one query in a database of the container and prints its single value.
query() {
  docker exec -e SQLCMDPASSWORD="$password" "$NAME" "$SQLCMD" -C -S localhost -U sa -b -h -1 -W \
    -d "$1" -Q "SET NOCOUNT ON; $2"
}

# Runs bf-maint and fails unless it exits with the code given first.
expect() {
  local wanted="$1"
  shift
  local got=0
  dotnet "$TOOL" "$@" >"$WORK/out.txt" 2>"$WORK/err.txt" || got=$?
  if [ "$got" != "$wanted" ]; then
    sed "s/$password/<password>/g" "$WORK/out.txt" "$WORK/err.txt" >&2
    fail "bf-maint $* exited $got, expected $wanted"
  fi
  echo "ok: bf-maint $* -> $got"
}

# Fails unless the last bf-maint run wrote this line to stdout.
expect_line() {
  grep -qxF -- "$1" "$WORK/out.txt" || { cat "$WORK/out.txt" >&2; fail "stdout has no line '$1'"; }
}

# Fails unless the last bf-maint run wrote this line to stderr.
expect_error_line() {
  grep -qxF -- "$1" "$WORK/err.txt" || { cat "$WORK/err.txt" >&2; fail "stderr has no line '$1'"; }
}

# Fails unless the query gives the value.
expect_value() {
  local got
  got="$(query "$1" "$2" | tr -d '\r')"
  [ "$got" = "$3" ] || fail "$4: got '$got', expected '$3'"
  echo "ok: $4 = $3"
}

echo "== Build bf-maint"
dotnet build src/Wolfgang.Barefoot.Maintenance -c Release -nologo -v q
TOOL="src/Wolfgang.Barefoot.Maintenance/bin/Release/net10.0/bf-maint.dll"
[ -f "$TOOL" ] || fail "the build did not make $TOOL"

echo "== Start SQL Server ($IMAGE)"
docker run -d --name "$NAME" -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$password" \
  -p "127.0.0.1:$PORT:1433" "$IMAGE" >/dev/null
ready=false
for _ in $(seq 1 60); do
  if query master "SELECT 1" >/dev/null 2>&1; then
    ready=true
    break
  fi
  sleep 2
done
if [ "$ready" != "true" ]; then
  docker logs --tail 30 "$NAME" >&2 || true
  fail "SQL Server did not accept a connection within two minutes"
fi
echo "server: $(query master "SELECT CAST(SERVERPROPERTY('ProductVersion') AS varchar(30))")"

connection() {
  echo "Server=127.0.0.1,$PORT;Database=$1;User Id=sa;Password=$password;TrustServerCertificate=True"
}
export ConnectionStrings__Barefoot
ConnectionStrings__Barefoot="$(connection barefoot)"

echo "== A database that does not exist is not created"
expect 8 migrate --status

echo "== Up"
query master "CREATE DATABASE barefoot" >/dev/null
expect 11 migrate --status
expect_line "state: MissingMigration"
expect 0 migrate
expect_line "direction: up"
expect 0 migrate --status
expect_line "state: UpToDate"
tables="$(query barefoot "SELECT COUNT(*) FROM sys.tables WHERE name <> '__EFMigrationsHistory'" | tr -d '\r')"
[ "$tables" -gt 0 ] || fail "no table was made"
echo "ok: tables after up = $tables"
expect 0 migrate
expect_line "direction: none"
expect 0 verify-schema
expect_line "differences: 0"

echo "== Down"
expect 6 migrate --to 0
expect 0 migrate --to 0 --confirm-data-loss
expect_line "direction: down"
expect_value barefoot "SELECT COUNT(*) FROM sys.tables WHERE name <> '__EFMigrationsHistory'" 0 "tables after down"
expect 11 migrate --status

echo "== Up again"
expect 0 migrate
expect_line "direction: up"
expect 0 migrate --status
expect_line "state: UpToDate"
expect_value barefoot "SELECT COUNT(*) FROM sys.tables WHERE name <> '__EFMigrationsHistory'" "$tables" "tables after the second up"

echo "== The unique text columns compare as .NET does"
binary="SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE COLLATION_NAME = 'Latin1_General_100_BIN2'"
expect_value barefoot "$binary" 7 "columns with the binary collation"
query barefoot "INSERT INTO recipient (id, owner_id, address, display_name, version) SELECT NEWID(), oid, N'a@example.com', N'', 1 FROM app_user UNION ALL SELECT NEWID(), oid, N'A@example.com', N'', 1 FROM app_user" >/dev/null
expect_value barefoot "SELECT COUNT(*) FROM recipient WHERE address = N'a@example.com'" 1 "rows that match an address in one case"
query barefoot "DELETE FROM recipient" >/dev/null

echo "== The script builds the same schema"
expect 0 migrate --script --output "$WORK/upgrade.sql"
query master "CREATE DATABASE barefoot_scripted" >/dev/null
docker cp "$WORK/upgrade.sql" "$NAME:/tmp/upgrade.sql" >/dev/null
docker exec -e SQLCMDPASSWORD="$password" "$NAME" "$SQLCMD" -C -S localhost -U sa -b \
  -d barefoot_scripted -i /tmp/upgrade.sql >"$WORK/script.txt" 2>&1 \
  || { cat "$WORK/script.txt" >&2; fail "sqlcmd could not apply the script"; }

columns="TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION, COLUMN_DEFAULT, COLLATION_NAME"
indexes="OBJECT_NAME(object_id) AS table_name, name, type_desc, is_unique, is_primary_key, is_unique_constraint, filter_definition"
keys="name, OBJECT_NAME(parent_object_id) AS table_name, OBJECT_NAME(referenced_object_id) AS referenced, delete_referential_action_desc"
for pair in "barefoot barefoot_scripted" "barefoot_scripted barefoot"; do
  # shellcheck disable=SC2086
  set -- $pair
  expect_value master "SELECT COUNT(*) FROM (SELECT $columns FROM $1.INFORMATION_SCHEMA.COLUMNS EXCEPT SELECT $columns FROM $2.INFORMATION_SCHEMA.COLUMNS) d" 0 "columns in $1 that differ in $2"
  expect_value master "SELECT COUNT(*) FROM (SELECT $indexes FROM $1.sys.indexes WHERE name IS NOT NULL EXCEPT SELECT $indexes FROM $2.sys.indexes WHERE name IS NOT NULL) d" 0 "indexes in $1 that differ in $2"
  expect_value master "SELECT COUNT(*) FROM (SELECT $keys FROM $1.sys.foreign_keys EXCEPT SELECT $keys FROM $2.sys.foreign_keys) d" 0 "foreign keys in $1 that differ in $2"
done
ConnectionStrings__Barefoot="$(connection barefoot_scripted)"
expect 0 migrate --status
expect_line "state: UpToDate"
expect 0 verify-schema
expect_line "differences: 0"

echo "== verify-schema names what was changed by hand"
dropped="$(query barefoot_scripted "SELECT TOP 1 t.name + '.' + i.name FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.name IS NOT NULL AND t.name <> '__EFMigrationsHistory' ORDER BY 1" | tr -d '')"
query barefoot_scripted "DROP INDEX [${dropped#*.}] ON [${dropped%%.*}]; ALTER TABLE app_user ADD stray_column int NULL; ALTER TABLE app_user ALTER COLUMN version bigint NULL; CREATE TABLE stray_table (id int NOT NULL);" >/dev/null
expect 1 verify-schema
expect_line "differences: 5"
expect_error_line "missing: index $dropped"
expect_error_line "extra: column app_user.stray_column"
expect_error_line "different: column app_user.version is bigint null, expected bigint not null"
expect_error_line "extra: table stray_table"
expect_error_line "extra: column stray_table.id"

echo "Migration check passed: up, down, up, the script and verify-schema, on SQL Server."
