using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// Makes a context build its model itself. EF otherwise builds a context type's model once in a
/// process and reuses it, so a test would read the model an earlier test built. A mutation test
/// changes the model-building code between runs in one process and has to see each change.
/// </summary>
internal static class FreshModelExtensions
{
    /// <summary>
    /// Gives every context made from these options services of its own, and so a model of its own.
    /// </summary>
    public static T UseFreshModel<T>(this T optionsBuilder)
        where T : DbContextOptionsBuilder
    {
        optionsBuilder
            .EnableServiceProviderCaching(false)
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        return optionsBuilder;
    }
}
