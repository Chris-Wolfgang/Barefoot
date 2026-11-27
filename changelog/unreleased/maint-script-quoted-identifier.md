type: fix

A script from `bf-maint migrate --script` starts with `SET QUOTED_IDENTIFIER ON;`. Without it `sqlcmd` failed on the first filtered index with error 1934.
