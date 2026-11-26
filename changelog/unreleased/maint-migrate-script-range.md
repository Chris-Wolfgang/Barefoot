type: feature

`bf-maint migrate --script` takes `--from` and `--to`, so it can write the SQL of part of the migrations or of a downgrade. A downgrade script starts with a `-- Downgrade` comment that names each step that loses data, and one that would lose data needs `--confirm-data-loss`.
