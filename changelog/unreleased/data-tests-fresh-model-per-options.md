type: internal

The Data tests share EF's services between contexts and rebuild only the model for each test database, so a mutation run does less work per test.
