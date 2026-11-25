type: feature

`BarefootDbContext` now sets `Version` on every save: a new row is stored with 1 and a changed row with the version it was read with plus one. A save made from an older version of a row fails instead of overwriting it.
