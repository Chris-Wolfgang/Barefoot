using System.Text;

namespace Wolfgang.Barefoot.Data.Conventions;

/// <summary>
/// Turns a C# name into the name the database uses (docs/DATABASE-CONVENTIONS.md, Names):
/// <c>SearchMatrix</c> is <c>search_matrix</c> and <c>OwnerId</c> is <c>owner_id</c>.
/// </summary>
internal static class SnakeCase
{
    /// <summary>
    /// The snake_case form of a Pascal-case name. A word starts at a capital that follows a small
    /// letter or a digit, and at the last capital of a run of capitals that a small letter follows.
    /// Digits stay with the word before them: <c>ResultSha256</c> is <c>result_sha256</c>.
    /// </summary>
    /// <param name="name">A type or property name.</param>
    public static string Of(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var text = new StringBuilder(name.Length + 8);

        for (var index = 0; index < name.Length; index++)
        {
            var letter = name[index];

            if (index > 0 && char.IsUpper(letter) && StartsWord(name, index))
            {
                text.Append('_');
            }

            text.Append(char.ToLowerInvariant(letter));
        }

        return text.ToString();
    }



    private static bool StartsWord(string name, int index)
    {
        var before = name[index - 1];

        if (!char.IsUpper(before))
        {
            return true;
        }

        return index + 1 < name.Length && char.IsLower(name[index + 1]);
    }
}
