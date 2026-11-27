using System.Runtime.InteropServices;

namespace Wolfgang.Barefoot.Business.Catalog;

/// <summary>
/// How long a room-vocabulary value has gone unseen, counted in runs (<see cref="VocabularyStaleness"/>).
/// </summary>
/// <param name="RunsSinceSeenCount">How many counted runs started after the value was last seen.</param>
/// <param name="IsStale">Whether that is enough for the search editor to grey the value.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct VocabularyAge(int RunsSinceSeenCount, bool IsStale);
