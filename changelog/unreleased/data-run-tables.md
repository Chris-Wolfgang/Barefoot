type: feature

Add the run tables to `BarefootDbContext`: runs, each search's part in a run, the room lookups and the links from searches to lookups. A filtered unique index is the run lock: the database stores at most one run in progress.
