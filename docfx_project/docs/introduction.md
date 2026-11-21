# What Barefoot is

The Sandals and Beaches booking site searches well when you know the resort, the departure airport
and the dates. It is tedious when any of those are flexible. Barefoot does the tedious part.

## What it does

- You save a **search**: resorts, departure airports, a check-in window (the same weeks every year,
  or one set of dates), the number of nights and who is travelling.
- About twice a week Barefoot expands every active search into its individual **lookups** and
  performs each one on the public booking site, in a real browser, one at a time.
- It stores every room option and every flight option it finds, so a price can be compared with
  what it was last week or last month.
- When a run is done it e-mails a **digest** per search: the cheapest options that match the
  filters you set, and what changed since the last run.
- A web site shows your searches, the results of each run, price history and past digests.

Several unrelated people can share one installation. Each sees only their own searches and results.

## What it is not

- It does not book anything. It reads what the public site shows.
- Its prices are not quotes. They are what the site displayed when the lookup ran.
- It is not affiliated with or endorsed by Sandals Resorts International.

## The parts

| Part | Where it runs | What it does |
|---|---|---|
| API | Azure | The only process that reads and writes the database. It holds the rules and checks who is calling. |
| Web site | Azure | Where people sign in, edit searches and read results. |
| `bf-scraper` | A home machine | Performs the lookups in a browser and sends the results to the API. |
| `bf` | Anywhere | The command-line tool: scriptable steps such as sending the digests. |
| `bf-maint` | An administrator's machine | Creates and migrates the database schema. |

The reasons for this shape are in
[the design](https://github.com/Chris-Wolfgang/barefoot/blob/main/docs/DESIGN.md).
