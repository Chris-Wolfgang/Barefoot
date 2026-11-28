type: internal

Code reads a setting through `ISettings`, by its typed key. Each API instance keeps the stored values in a cache and asks the database whether they changed only on a read, and only when the cache is older than 30 seconds. Nothing reads a setting yet.
