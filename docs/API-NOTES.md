# Booking site notes

What `Wolfgang.Barefoot.Obe` needs to know about the public booking site at `obe.sandals.com`: the
page flow, the text and markup of the room and flight cards, and the traps in each. These notes are
taken from the earlier tool's notes, observed in July 2026, and redacted for this repository
(sandals-search#261).

*This is an independent tool and is not affiliated with or endorsed by Sandals Resorts International.*

## How the site is read

- The scraper opens the booking pages in a real browser (Chromium through Playwright) and reads
  what the page shows a visitor: the rendered room cards and flight cards.
- It fills in the same form a person fills in. It calls none of the site's own endpoints directly.
- It never books. It stops before any step that commits a choice (**SELECT FLIGHTS & CONTINUE** is
  never clicked).
- It is polite: one lookup at a time, with a delay and jitter between lookups (DESIGN §3).
- Everything in this document can change without notice. Every selector and gesture lives in `Obe`
  and nowhere else, so that a change on the site is fixed in one place.

## The steps of the site

`Vacation → Room → Flights → Guests → Payment`. The scraper uses the first three.

| Step | Path | What is read |
|---|---|---|
| Vacation | `/room/` (the landing form) | the resort list and the airport list (catalogs) |
| Room | `/room/` after **Continue** | the room cards: one priced or sold-out card per room category |
| Room details | `/room-details/` | nothing; it is the way to the Flights step |
| Flights | `/itineraries/` | the itinerary cards: one per round trip |

- A session belongs to one visit. Opening the Room step directly, without a vacation chosen in that
  session, returns the visitor to the Vacation step. The driver therefore walks the form once and
  then changes dates in place.
- A session expires. A long run must start a new one.

## Catalogs

The Vacation page carries three lists in its own page data:

| List | Members | Used for |
|---|---|---|
| Resorts | resort code, name, country (the island) | `Resort` |
| Resort gateways | resort code, country, city, gateway airport code | `Resort.GatewayAirportId` |
| Airports | airport code, city, state, country, airport name (about 400) | `Airport` |

- An airport is a **gateway** when it is some resort's gateway, and a **departure** airport otherwise.
- Sync the airports before the resorts, so that a resort's gateway airport exists when the resort is
  written.
- The resort drop-down (`select[name='rstCode']`) lists the same codes and names. It is the fallback
  if the page data changes shape.
- Seventeen resorts were listed. Examples of gateways: the St. Lucia resorts use `UVF`, Antigua
  `ANU`, the Jamaica resorts `MBJ`.

## The Vacation form

Almost every control exists twice in the page: a hidden copy for small screens and the visible one.
**Always use the visible copy.** The first match is usually the hidden one, and it never updates.

1. **Resort**: `select[name='rstCode']`. Wait until it has its options, then select by option
   value, which is the resort code. The label is the fallback.
2. **Open the calendar**: click the visible **Check-In Date** field, then press `Enter`. A click
   alone does not open it. It is open when a visible `[data-date='YYYY-MM-DD']` cell exists.
3. **Check-in**: the day cells are
   `div[role=button][data-testid='calendar-cell-ui'][data-date='YYYY-MM-DD']`, with an
   `aria-label` such as `Sunday, August 9, 2026`. Let the opening animation finish (about 600 ms),
   then click the check-in cell. The field shows the date at once.
4. **Check-out**: the picker takes the *hovered* date as the end of the range. Hover the end cell,
   wait about 300 ms, then click. The calendar shows the range, but the field still reads
   **Check-Out Date** until the calendar closes.
5. **Close the calendar**: click outside it (`Escape` is the fallback). The check-out field then
   shows the date. Read it back from the visible `#datesForm`.
6. **Flights**: two radio buttons, `input[name='flightsForm.airIncluded']`.
   - Without flights: choose **No, thank you**.
   - With flights: choose **Yes, please**, type a city into
     `input[name='flightsForm.departureGateway']`, and click the suggestion
     `[role=option][data-key='<airport code>']`. Its text reads like
     `CITY, STATE(ST), USA (XXX)`.
   - With **Yes, please** chosen and no departure airport, **Continue** is refused.
7. **Continue**: the button named `CONTINUE`.

## Room cards

After **Continue** the room list loads late: a spinner first, then the cards. Wait for the text
`Category Code` (allow up to 45 seconds).

One card per room category:

```
<room name>                    "Grande Rondoval Butler Suite with Private Pool Sanctuary"
Category Level: <level> Level  Butler | Club | Luxury
Category Code: <code>          RP, OWB, LX, WOL, 1BL
<availability>                 "Room All–Inclusive Price: $14,044"
                               or "4 ROOMS LEFT" with a price
                               or "SOLD OUT for your dates"
[ROOM DETAILS | FIND AVAILABILITY]
```

To find the cards: take each leaf node that contains `Category Code:` and walk up, at most six
parents, to the first element that has both `Category Level` and either a price or `SOLD OUT`.

What a card gives:

| Field | From | Notes |
|---|---|---|
| Room name | the first line | free text |
| Level | `Category Level:` | Butler, Club or Luxury, without the word "Level" |
| Category code | `Category Code:` | short and stable within a resort; two to three characters, letters and digits |
| Total price | the first `$…` on the card | whole stay, all guests, taxes included, USD; no cents were seen on room cards |
| Sold out | the text `SOLD OUT` | a sold-out card has no price |
| Rooms left | `N ROOMS LEFT` | optional; present only when few remain |

- **Room view, room type and room features are not on the card.** They are the filter groups on the
  left of the Room step (*Room Level*, *Room View*, *Room Types*, *Room Features*). Which rooms a
  value applies to has not been captured yet; see [Open points](#open-points).
- The dash in `All–Inclusive` is an en dash, not a hyphen.
- The room list for one resort held 19 categories in the sample.

### Vocabulary seen

| Group | Values seen |
|---|---|
| Level | Butler, Club, Luxury |
| View | Tropical Garden, Private Pool, Beachfront, Oceanfront, Oceanview, Pool, Partial Ocean |
| Type | Private Pool Suite, Rondoval Villa, Villa Suite, Over-The-Water Bungalow, Walkout Suite, One-Bedroom Suite, Penthouse |
| Feature | Patio, Tranquility Soaking Tub, Balcony, Wheelchair Accessible, Patio or Balcony |

The lists are examples, not a closed set: vocabularies are discovered from results and never
authored (DESIGN §7).

## Reaching the Flights step

1. Fill in the Vacation form with flights and a departure airport, and continue to the Room step.
2. Click **Room Details** on a priced room. The page goes to `/room-details/`.
3. Click **SELECT ROOM & CONTINUE**. The page goes to `/itineraries/`, headed **SELECT FLIGHTS**.
   The itineraries load late; wait for the spinner to go.

The list of flights does not depend on the room. Choose the cheapest available room only to reach
the step, and read the itineraries once per departure airport, gateway, dates, cabin and guests.

A browser without a window did not follow the **Room Details** link when this was first tried;
a browser with a window did. Develop and tune the driver with a window.

## Itinerary cards

- The heading reads `<ORIGIN CITY> (XXX) - <GATEWAY NAME> (YYY)`: departure airport code, then
  gateway airport code.
- A count line reads `N ROUNDTRIPS MATCHING YOUR PREFERENCES` (50 in the sample).
- Each round trip is `[data-testid='card-itinerary-0']`, `card-itinerary-1`, and so on. Iterate
  `[data-testid^='card-itinerary-']`.

Inside a card, by `data-testid`:

| `data-testid` | Content |
|---|---|
| `card-itinerary-summary-ui` | the summary; click it to show the legs (`aria-expanded` changes) |
| `card-details-toggle-button-ui` | also toggles the legs |
| `room-flight-price` | the **room + flights** total, for example `$15,750.66` |
| `itinerary-stops` | `1 Stop` |
| `itinerary-overnight` | `- Overnight`, present only when the direction crosses midnight |
| `departure-time` | two per card: the outbound and the return departure time |
| `departing-details-ui` | the outbound legs |
| `returning-details-ui` | the return legs |
| `detail-card-ui` | one leg |

The summary's text, line by line: `Leaving`, departure time, origin code, arrival time, destination
code, `K Stop(s)`; then `Returning` (or `Returning - Overnight`) with the same five; then
`ROOM + FLIGHTS (USD)` and the total; then `SHOW DETAILS`.

Inside a leg (`detail-card-ui`):

| `data-testid` | Text pattern | Gives |
|---|---|---|
| `flight-number-ui` | `<Carrier> Flight # <number>  <Weekday>, <Mon> <dd>, <yyyy>  <Cabin name> (<fare code>)` | carrier (before ` Flight #`), flight number, date, cabin name, fare code. The date may be followed by ` - Overnight Flight` |
| `departing-ui` | `Departing:  <hh:mm AM/PM> <Airport name> (<code>)` | local departure time and airport |
| `arriving-ui` | `Arriving:  <hh:mm AM/PM> <Airport name> (<code>)  Flight operated by <Carrier> \|` | local arrival time, airport, operating carrier |

A connection is marked `Stop 1 - <CITY> (<code>)` between legs.

What an itinerary gives:

| Field | From | Notes |
|---|---|---|
| Room + flights total | `room-flight-price` | USD, with cents |
| Airfare | total − the room-only price of the same room | derived; the page shows no air-only figure. Example: $15,750.66 − $14,044 = $1,706.66 |
| Per direction: stops | legs − 1 | `itinerary-stops` says the same |
| Per direction: overnight | `itinerary-overnight` | |
| Per leg | the three elements above | marketing carrier, flight number, date, cabin name, fare code, departure time and airport, arrival time and airport, operating carrier |
| Per direction: travel time | first departure to last arrival | derived. The times are local to each airport, so the arithmetic needs the airports' time zones (DESIGN §7) |

## Text formats and their traps

| What | Pattern | Trap |
|---|---|---|
| Money | `$` then digits with thousands commas, optionally `.` and two digits | room cards show whole dollars, itinerary cards show cents; a sold-out card has no money at all |
| Airport code | three capital letters in parentheses at the end of an airport name | a city or state can also appear in parentheses: `STATE(ST)` has two letters |
| Time | `hh:mm` then `AM` or `PM`, with or without a space | twelve-hour clock; `12:05 AM` is five past midnight |
| Flight number | `Flight #` then digits | the carrier name before it can contain spaces |
| Date | `<Weekday>, <Mon> <dd>, <yyyy>` | English month abbreviations; may be followed by ` - Overnight Flight` |
| Rooms left | digits then ` ROOMS LEFT` | only the plural was seen; expect `1 ROOM LEFT` too |

These are descriptions, not the expressions to use. The parsers are written with anchored,
`NonBacktracking`, time-limited regular expressions and tested against captured markup (DESIGN §3).

## Open points

1. **Which view, type and features a room has.** They are filter groups, not card text. Either the
   room-details page lists them or applying each filter shows which cards remain. Not captured.
2. **Whether a card ever shows a promotion or a struck-through price.** None was seen in the sample.
3. **Children and infants.** Every capture was for two adults. Whether the form asks for ages is not known.
4. **Cabin choice.** The sample showed economy fares. Where the cabin is chosen on the form was not captured.
5. **The exact page data shape of the three catalog lists.** Capture it as a fixture when `Obe` is built.
6. **Beaches resorts.** Not looked at (ADR 0009 §2).
