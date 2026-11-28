type: feature

Add exit code 11, `MissingMigration`: `bf-maint migrate --status` will exit with it when the database is behind the build.
