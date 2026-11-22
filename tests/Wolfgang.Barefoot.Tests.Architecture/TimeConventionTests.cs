using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// Time is a UTC <see cref="DateTimeOffset"/> everywhere except display. No public member of an assembly
/// under <c>src/</c> exposes <see cref="DateTime"/>, whose offset is a guess the reader has to make.
/// </summary>
public sealed class TimeConventionTests
{
    [Fact]
    public void DateTimeExposuresIn_when_given_the_source_assemblies_reports_nothing()
    {
        Assert.Empty(CompiledCodeRules.DateTimeExposuresIn(SourceAssemblies.All));
    }



    [Fact]
    public void DateTimeExposuresIn_when_a_public_surface_uses_DateTime_reports_every_member()
    {
        var fixture = FixtureCompiler.Compile
        (
            "DateTimeFixture",
            """
            using System;
            using System.Collections.Generic;

            namespace Fixture;

            public sealed class UsesDateTime
            {
                public UsesDateTime(DateTime when) { When = when; }

                public DateTime When { get; }

                public IReadOnlyList<DateTime?> History { get; init; } = [];

                public DateTime[] Recent = [];

                public DateTime Next() => When;

                public static string Shift(ref DateTime start) => start.ToString("O");

                private DateTime Hidden() => When;
            }

            public sealed record UsesDateTimeOffset(DateTimeOffset When, IReadOnlyList<DateTimeOffset?> History, DateOnly CheckIn)
            {
                public DateTimeOffset Next() => When;
            }

            internal sealed class NotPublic
            {
                public DateTime When { get; set; }
            }
            """
        );

        Assert.Equal
        (
            [
                "Fixture.UsesDateTime..ctor(when)",
                "Fixture.UsesDateTime.History",
                "Fixture.UsesDateTime.Next()",
                "Fixture.UsesDateTime.Recent",
                "Fixture.UsesDateTime.Shift(start)",
                "Fixture.UsesDateTime.When",
            ],
            CompiledCodeRules.DateTimeExposuresIn([fixture])
        );
    }
}
