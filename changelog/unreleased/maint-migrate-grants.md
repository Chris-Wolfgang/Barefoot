type: feature

`bf-maint migrate` runs `grant-roles` when the database ends on the latest migration, and reports it in a fifth line on stdout, `roles:` (`granted`, `skipped (...)` or `not run`).
