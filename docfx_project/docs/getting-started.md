# Running Barefoot

Barefoot is not released yet, so there is nothing to install today. This page says what an
installation will consist of. Each step gets its own page here when the feature behind it is built.

## What an installation needs

| Need | Used for |
|---|---|
| An Azure subscription | The database (Azure SQL Database, free offer) and the two containers (API and web site), which stop when idle |
| A Microsoft Entra tenant | Sign-in. People are invited as guests; there is no sign-up page and no stored password |
| A machine at home that can stay on twice a week | `bf-scraper`, which needs a browser and a residential connection |
| A mailbox that can send | The digests |

## The steps, in order

1. Create the database and the two containers.
2. Register the application in Entra and invite the people who will use it.
3. Create the schema with `bf-maint migrate`.
4. Schedule the semiweekly job on the home machine: refresh the airport and resort lists, run the
   lookups, send the digests.
5. Sign in to the web site and create a search.

## Until those pages exist

The design document describes each of these in full:
[DESIGN.md](https://github.com/Chris-Wolfgang/barefoot/blob/main/docs/DESIGN.md) — the database in
§11, sign-in in §10, the scheduled job in §5.
