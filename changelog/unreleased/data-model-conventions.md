type: feature

Add `ModelConventions.Verify`, which lists every place where a model breaks the database conventions: names, the default schema, `Guid` keys, `Restrict` on every foreign key, `Version` as a concurrency token, and instants stored in UTC. A test asserts that it finds nothing in `BarefootDbContext`'s model.
