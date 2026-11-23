using System.Globalization;
using System.Net;
using System.Text;

namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// Lays a digest out as an e-mail: a subject, an HTML part and a plain-text part (DESIGN §9;
/// ADR 0006 §6).
/// </summary>
/// <remarks>
/// A pure function. The markup is read on a phone: one column at most 600 px wide, 16 px text, one
/// card per room, tables and inline styles only, because mail programs ignore style sheets. Every
/// value from the model is encoded before it enters the HTML. The output does not depend on the
/// machine's culture, and its lines end in a line feed.
/// </remarks>
public static class DigestRenderer
{
    private const string Ink = "#1f2933";

    private const string Muted = "#5f6b76";

    private const string Line = "#d9dee3";

    private const string Accent = "#0b6e99";

    private const string Down = "#1a7f37";

    private const string Up = "#b42318";

    private const string Font = "font-family:Segoe UI,Helvetica,Arial,sans-serif";



    /// <summary>
    /// Renders <paramref name="digest"/>.
    /// </summary>
    /// <param name="digest">What the digest says.</param>
    /// <exception cref="ArgumentNullException"><paramref name="digest"/> is <see langword="null"/>.</exception>
    public static RenderedDigest Render(DigestModel digest)
    {
        ArgumentNullException.ThrowIfNull(digest);

        return new RenderedDigest(Subject(digest), Html(digest), Text(digest));
    }



    /// <summary>
    /// An amount in US dollars: whole dollars without cents, anything else with two decimals.
    /// </summary>
    /// <param name="amount">The amount.</param>
    public static string Money(decimal amount)
    {
        var format = amount == decimal.Truncate(amount) ? "N0" : "N2";

        return "$" + amount.ToString(format, CultureInfo.InvariantCulture);
    }



    /// <summary>
    /// How a price moved, in words a reader takes in at a glance: <c>▼ $486</c>, <c>▲ $120</c> or
    /// <c>unchanged</c>; <see langword="null"/> when there is nothing to compare with.
    /// </summary>
    /// <param name="change">The movement: negative is cheaper.</param>
    public static string? Change(decimal? change)
    {
        return change switch
        {
            null => null,
            0 => "unchanged",
            < 0 => "▼ " + Money(-change.Value),
            _ => "▲ " + Money(change.Value),
        };
    }



    private static string Subject(DigestModel digest)
    {
        var cheapest = digest
            .Resorts
            .Where(resort => resort.FromAmount.HasValue)
            .Select(resort => resort.FromAmount!.Value)
            .DefaultIfEmpty(-1)
            .Min();

        return cheapest < 0
            ? $"{digest.SearchName}: no matches"
            : $"{digest.SearchName}: {Matches(digest.MatchCount)} from {Money(cheapest)}";
    }



    private static string Matches(int count)
    {
        return count == 1
            ? "1 match"
            : string.Create(CultureInfo.InvariantCulture, $"{count:N0} matches");
    }



    private static string SearchedLine(DigestModel digest)
    {
        var day = digest.SearchedOn.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);
        var options = digest.MatchCount == 1
            ? "1 room option matches your filters"
            : string.Create(CultureInfo.InvariantCulture, $"{digest.MatchCount:N0} room options match your filters");

        return $"Searched {day} · {options}";
    }



    private static string StayLine(DigestRoom room)
    {
        var day = room.CheckIn.ToString("ddd MMM d", CultureInfo.InvariantCulture);
        var nights = room.NightCount == 1 ? "1 night" : string.Create(CultureInfo.InvariantCulture, $"{room.NightCount} nights");

        return $"Check-in {day} · {nights}";
    }



    private static string More(DigestResort resort)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{resort.MoreCount:N0} more at {resort.Name}");
    }



    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }



    private static string Html(DigestModel digest)
    {
        var html = new StringBuilder();

        html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        html.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">\n");
        html.Append("<meta name=\"color-scheme\" content=\"light dark\">\n");
        html.Append(CultureInfo.InvariantCulture, $"<title>{Encode(digest.SearchName)}</title>\n</head>\n");
        html.Append(CultureInfo.InvariantCulture, $"<body style=\"margin:0;padding:0;background:#f3f5f7;color:{Ink};{Font};font-size:16px;line-height:1.45\">\n");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr><td align=\"center\" style=\"padding:12px\">\n");
        html.Append(CultureInfo.InvariantCulture, $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:600px;background:#ffffff;border:1px solid {Line}\">\n");

        html.Append(CultureInfo.InvariantCulture, $"<tr><td style=\"padding:16px 20px;border-bottom:1px solid {Line}\">");
        html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:18px;font-weight:700\">{Encode(digest.ProductName)}</div>");
        html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:13px;color:{Muted}\">{Encode(digest.Tagline)}</div></td></tr>\n");

        html.Append("<tr><td style=\"padding:20px\">\n");
        html.Append(CultureInfo.InvariantCulture, $"<h1 style=\"margin:0;font-size:22px\">{Encode(digest.SearchName)}</h1>\n");
        html.Append(CultureInfo.InvariantCulture, $"<div style=\"color:{Muted};font-size:14px;margin:4px 0 16px\">{Encode(SearchedLine(digest))}</div>\n");

        foreach (var sentence in digest.Attention)
        {
            html.Append("<div style=\"background:#fff4e5;border-left:4px solid #b54708;padding:10px 12px;margin:0 0 12px\">");
            html.Append(CultureInfo.InvariantCulture, $"<b>Needs your attention.</b> {Encode(sentence)}</div>\n");
        }

        AppendHeading(html, "Summary");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">\n");

        foreach (var resort in digest.Resorts)
        {
            html.Append(CultureInfo.InvariantCulture, $"<tr><td style=\"padding:6px 0;border-bottom:1px solid {Line}\">{Encode(resort.Name)}</td>");
            html.Append(CultureInfo.InvariantCulture, $"<td align=\"right\" style=\"padding:6px 0;border-bottom:1px solid {Line};white-space:nowrap\">{SummaryPriceHtml(resort)}</td></tr>\n");
        }

        html.Append("</table>\n");
        AppendButton(html, digest.ResultsUrl, $"Open in {digest.ProductName}", large: true);

        AppendHeading(html, "What was searched");
        html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:14px;color:{Muted}\">");
        html.AppendJoin("<br>", digest.Criteria.Select(Encode));
        html.Append("</div>\n");

        foreach (var resort in digest.Resorts.Where(resort => resort.Rooms.Count > 0))
        {
            AppendResort(html, resort);
        }

        html.Append("</td></tr>\n");
        html.Append(CultureInfo.InvariantCulture, $"<tr><td style=\"padding:16px 20px;border-top:1px solid {Line};font-size:12px;color:{Muted}\">");
        html.Append(Encode(Footer(digest)));
        html.Append("<br>");
        html.Append(Encode(Disclaimer(digest)));
        html.Append("</td></tr>\n</table>\n</td></tr></table>\n</body>\n</html>\n");

        return html.ToString();
    }



    private static void AppendHeading(StringBuilder html, string text)
    {
        html.Append(CultureInfo.InvariantCulture, $"<h2 style=\"margin:20px 0 8px;font-size:13px;letter-spacing:.06em;text-transform:uppercase;color:{Muted}\">{Encode(text)}</h2>\n");
    }



    private static void AppendButton
    (
        StringBuilder html,
        Uri url,
        string label,
        bool large
    )
    {
        var padding = large ? "12px 18px" : "10px 14px";
        var style = large
            ? $"background:{Accent};color:#ffffff"
            : $"border:1px solid {Accent};color:{Accent}";

        html.Append(CultureInfo.InvariantCulture, $"<div style=\"margin:14px 0\"><a href=\"{Encode(url.AbsoluteUri)}\" style=\"display:inline-block;padding:{padding};{style};font-weight:600;text-decoration:none;border-radius:6px\">{Encode(label)}</a></div>\n");
    }



    private static string SummaryPriceHtml(DigestResort resort)
    {
        if (resort.IsOffline)
        {
            return $"<span style=\"color:{Muted}\">offline</span>";
        }

        if (resort.FromAmount is not { } price)
        {
            return $"<span style=\"color:{Muted}\">no matches</span>";
        }

        var change = Change(resort.ChangeAmount);
        var colour = resort.ChangeAmount switch
        {
            < 0 => Down,
            > 0 => Up,
            _ => Muted,
        };

        return change is null
            ? $"<b>{Money(price)}</b>"
            : $"<b>{Money(price)}</b> &nbsp;<span style=\"color:{colour};font-size:14px\">{Encode(change)}</span>";
    }



    private static void AppendResort(StringBuilder html, DigestResort resort)
    {
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:24px 0 8px\"><tr>");
        html.Append(CultureInfo.InvariantCulture, $"<td style=\"font-size:17px;font-weight:700;border-bottom:2px solid {Ink};padding-bottom:4px\">{Encode(resort.Name)}</td>");
        html.Append(CultureInfo.InvariantCulture, $"<td align=\"right\" style=\"font-size:15px;border-bottom:2px solid {Ink};padding-bottom:4px;white-space:nowrap\">from {Money(resort.Rooms[0].TotalAmount)}</td>");
        html.Append("</tr></table>\n");

        foreach (var room in resort.Rooms)
        {
            html.Append(CultureInfo.InvariantCulture, $"<div style=\"border:1px solid {Line};border-radius:8px;padding:12px 14px;margin:0 0 10px\">\n");
            html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:24px;font-weight:700\">{Money(room.TotalAmount)}");

            if (room.TotalNote is { } note)
            {
                html.Append(CultureInfo.InvariantCulture, $" <span style=\"font-size:13px;font-weight:400;color:{Muted}\">{Encode(note)}</span>");
            }

            html.Append("</div>\n");
            html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-weight:600;margin-top:4px\">{Encode(room.RoomName)}</div>\n");

            if (room.Amenities.Count > 0)
            {
                html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:14px;color:{Muted}\">{Encode(string.Join(" · ", room.Amenities))}</div>\n");
            }

            html.Append(CultureInfo.InvariantCulture, $"<div style=\"display:inline-block;margin:8px 0;padding:3px 10px;border:1px solid {Line};border-radius:12px;font-size:13px\">{Encode(StayLine(room))}</div>\n");
            html.Append(CultureInfo.InvariantCulture, $"<div style=\"font-size:14px\">Room <b>{Money(room.RoomAmount)}</b>");

            if (room.PreviousRoomAmount is { } before)
            {
                html.Append(CultureInfo.InvariantCulture, $" <s style=\"color:{Muted}\">{Money(before)}</s>");
            }

            if (room.Promotion is { } promotion)
            {
                html.Append(CultureInfo.InvariantCulture, $" · {Encode(promotion)}");
            }

            if (room.Flights.Count > 0)
            {
                html.Append("<br>Flights ");
                html.AppendJoin(" · ", room.Flights.Select(flight => $"{Encode(flight.AirportCode)} <b>{Money(flight.AirfareAmount)}</b>"));
            }

            html.Append("</div>\n</div>\n");
        }

        if (resort.MoreCount > 0)
        {
            AppendButton(html, resort.ResultsUrl, More(resort), large: false);
        }
    }



    private static string Footer(DigestModel digest)
    {
        return $"You are getting this because you have an active search in {digest.ProductName}. Prices come from the public booking site at the time of the search and can change at any time; they are not quotes.";
    }



    private static string Disclaimer(DigestModel digest)
    {
        return $"{digest.ProductName} is an independent tool and is not affiliated with or endorsed by Sandals Resorts International.";
    }



    private static string Text(DigestModel digest)
    {
        var text = new StringBuilder();

        text.Append(CultureInfo.InvariantCulture, $"{digest.ProductName}\n{digest.Tagline}\n\n");
        text.Append(CultureInfo.InvariantCulture, $"{digest.SearchName}\n{SearchedLine(digest)}\n");

        foreach (var sentence in digest.Attention)
        {
            text.Append(CultureInfo.InvariantCulture, $"\nNEEDS YOUR ATTENTION: {sentence}\n");
        }

        text.Append("\nSUMMARY\n");

        foreach (var resort in digest.Resorts)
        {
            text.Append(CultureInfo.InvariantCulture, $"- {resort.Name}: {SummaryPriceText(resort)}\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"\nOpen in {digest.ProductName}: {digest.ResultsUrl.AbsoluteUri}\n");
        text.Append("\nWHAT WAS SEARCHED\n");

        foreach (var line in digest.Criteria)
        {
            text.Append(CultureInfo.InvariantCulture, $"{line}\n");
        }

        foreach (var resort in digest.Resorts.Where(resort => resort.Rooms.Count > 0))
        {
            text.Append(CultureInfo.InvariantCulture, $"\n{resort.Name.ToUpperInvariant()} (from {Money(resort.Rooms[0].TotalAmount)})\n");

            foreach (var room in resort.Rooms)
            {
                AppendRoomText(text, room);
            }

            if (resort.MoreCount > 0)
            {
                text.Append(CultureInfo.InvariantCulture, $"\n{More(resort)}: {resort.ResultsUrl.AbsoluteUri}\n");
            }
        }

        text.Append(CultureInfo.InvariantCulture, $"\n--\n{Footer(digest)}\n{Disclaimer(digest)}\n");

        return text.ToString();
    }



    private static string SummaryPriceText(DigestResort resort)
    {
        if (resort.IsOffline)
        {
            return "offline";
        }

        if (resort.FromAmount is not { } price)
        {
            return "no matches";
        }

        return Change(resort.ChangeAmount) is { } change
            ? $"{Money(price)} ({change})"
            : Money(price);
    }



    private static void AppendRoomText(StringBuilder text, DigestRoom room)
    {
        text.Append(CultureInfo.InvariantCulture, $"\n{Money(room.TotalAmount)}");

        if (room.TotalNote is { } note)
        {
            text.Append(CultureInfo.InvariantCulture, $" {note}");
        }

        text.Append(CultureInfo.InvariantCulture, $"\n  {room.RoomName}\n");

        if (room.Amenities.Count > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"  {string.Join(" · ", room.Amenities)}\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"  {StayLine(room)}\n  Room {Money(room.RoomAmount)}");

        if (room.PreviousRoomAmount is { } before)
        {
            text.Append(CultureInfo.InvariantCulture, $" (was {Money(before)})");
        }

        if (room.Promotion is { } promotion)
        {
            text.Append(CultureInfo.InvariantCulture, $" · {promotion}");
        }

        text.Append('\n');

        if (room.Flights.Count > 0)
        {
            text.Append("  Flights ");
            text.AppendJoin(" · ", room.Flights.Select(flight => $"{flight.AirportCode} {Money(flight.AirfareAmount)}"));
            text.Append('\n');
        }
    }
}
