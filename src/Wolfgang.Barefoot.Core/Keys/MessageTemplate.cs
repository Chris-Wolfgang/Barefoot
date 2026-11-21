namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// Reads a message template the way <see cref="string.Format(IFormatProvider, string, object[])"/> does,
/// so a template that would throw there is refused when its key is constructed instead.
/// </summary>
/// <remarks>
/// A template holds text, positional placeholders (<c>{0}</c>, optionally <c>{0,8}</c> or <c>{0:N2}</c>)
/// and doubled braces for a literal brace. Placeholders are numbered from zero with no gap.
/// </remarks>
internal static class MessageTemplate
{
    /// <summary>
    /// The number of arguments <paramref name="template"/> takes.
    /// </summary>
    /// <param name="template">The template to read.</param>
    /// <exception cref="FormatException">
    /// The template has a single brace, a placeholder that is not positional, or a gap in its numbering.
    /// </exception>
    public static int CountArguments(string template)
    {
        var indexes = new SortedSet<int>();
        var position = 0;

        while (position < template.Length)
        {
            var current = template[position];
            var doubled = position + 1 < template.Length && template[position + 1] == current;

            if (current is '{' or '}' && doubled)
            {
                position += 2;
            }
            else if (current == '{')
            {
                position = ReadPlaceholder(template, position, indexes);
            }
            else if (current == '}')
            {
                throw new FormatException($"A single '}}' at position {position}. Write '}}}}' for a literal brace.");
            }
            else
            {
                position++;
            }
        }

        if (indexes.Count > 0 && indexes.Max != indexes.Count - 1)
        {
            throw new FormatException($"The placeholders are not numbered 0 to {indexes.Count - 1} with no gap.");
        }

        return indexes.Count;
    }



    /// <summary>
    /// Reads the placeholder that opens at <paramref name="open"/>, records its index and returns the
    /// position after its closing brace.
    /// </summary>
    /// <exception cref="FormatException">The placeholder is not closed, is not positional, or contains a brace.</exception>
    private static int ReadPlaceholder(string template, int open, SortedSet<int> indexes)
    {
        var close = template.IndexOf('}', open);
        if (close < 0)
        {
            throw new FormatException($"The '{{' at position {open} is never closed. Write '{{{{' for a literal brace.");
        }

        var body = template.AsSpan(open + 1, close - open - 1);
        var end = body.IndexOfAny(',', ':');
        var number = end < 0 ? body : body[..end];

        if (number.Length == 0 || number.ContainsAnyExceptInRange('0', '9') || number.Length > 2)
        {
            throw new FormatException($"The placeholder at position {open} is not positional. Write '{{0}}', '{{1}}' and so on.");
        }

        if (body.Contains('{'))
        {
            throw new FormatException($"The placeholder at position {open} contains a '{{'.");
        }

        indexes.Add(int.Parse(number, System.Globalization.CultureInfo.InvariantCulture));

        return close + 1;
    }
}
