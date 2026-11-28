type: change

Exit codes 10 (`Refused`) and 11 (`MissingMigration`) now belong to all three tools and stop the scheduled job: a database that is behind or ahead of the build stops the work until someone has acted.
