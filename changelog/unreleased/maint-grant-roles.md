type: feature

`bf-maint grant-roles` makes the database user `barefoot-api` for the API's managed identity and adds it to `db_datareader` and `db_datawriter`. Running it again changes nothing, and on a server that is not Azure SQL Database it says that it skipped the grant.
