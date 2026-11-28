namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Compares two descriptions of a schema, each a list of lines <c>item|detail</c>
/// (<see cref="SchemaLines"/>): what the model expects and what the database has.
/// </summary>
internal static class SchemaDrift
{
    /// <summary>What separates an item from its detail in a line.</summary>
    public const char Separator = '|';



    /// <summary>
    /// One line per difference, sorted by item: <c>missing: item</c> for an item only the model has,
    /// <c>extra: item</c> for one only the database has, and
    /// <c>different: item is detail, expected detail</c> for one both have with another detail.
    /// </summary>
    /// <param name="expected">The lines of the model.</param>
    /// <param name="actual">The lines of the database.</param>
    public static IReadOnlyList<string> Compare(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var wanted = ByItem(expected);
        var found = ByItem(actual);

        return wanted
            .Keys
            .Union(found.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(item => Difference(item, wanted.GetValueOrDefault(item), found.GetValueOrDefault(item)))
            .OfType<string>()
            .ToList();
    }



    private static string? Difference(string item, string? wanted, string? found)
    {
        if (found is null)
        {
            return $"missing: {item}";
        }

        if (wanted is null)
        {
            return $"extra: {item}";
        }

        return string.Equals(wanted, found, StringComparison.Ordinal)
            ? null
            : $"different: {item} is {found}, expected {wanted}";
    }



    private static Dictionary<string, string> ByItem(IEnumerable<string> lines)
    {
        var items = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            var cut = line.IndexOf(Separator, StringComparison.Ordinal);

            items[cut < 0 ? line : line[..cut]] = cut < 0 ? string.Empty : line[(cut + 1)..];
        }

        return items;
    }
}
