type: feature

`bf-maint migrate --to <target>` moves the schema up or down to a migration named by id, name, prefix or `0`, and says which direction it took. A move down that would lose data needs `--confirm-data-loss`.
