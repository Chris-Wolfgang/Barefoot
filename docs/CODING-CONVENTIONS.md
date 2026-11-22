# Coding conventions

Every rule on this page names what enforces it: an analyzer, a test, or a line a reviewer checks. A rule
with nothing behind it is not a rule, so none is written here without one. The design these rules serve is
`docs/DESIGN.md`; the reasons are in `docs/adr/`.

Some of the tests named below arrive with a later build phase (DESIGN §13). Where that is so the table says
which phase. Until that phase the rule is held by review.

## How a rule is enforced

| Kind | Where it lives | What a violation does |
|---|---|---|
| Analyzer | `.editorconfig`, `src/.editorconfig`, `tests/.editorconfig`, `Directory.Build.props` | Fails the build. Release builds treat every warning as an error; the rules marked *error* below fail a Debug build too. |
| Banned symbol | `BannedSymbols.txt` (`RS0030`, an error under `src/` and `tests/`) | Fails the build at the line that names the symbol. |
| Architecture test | `tests/Wolfgang.Barefoot.Tests.Architecture` | Fails the test run. Each scanner has a fixture that must fail and a test that asserts it does, so a scanner that stops finding anything is noticed. |
| Review | the pull-request checklist in `docs/DELIVERY-PROCESS.md` | The PR is not approved. Used only where no tool can decide. |

## Suppressing a warning

A suppression is a decision, and the decision must be readable next to the code it affects.

- **Narrow:** `#pragma warning disable XXXX` and `restore` around the specific lines, or
  `[SuppressMessage(..., Justification = "...")]` on the member. Either carries a one-line reason.
- **Wide** (a severity change in an `.editorconfig`, `<NoWarn>`): only with the reason written in the file.
  Those files are protected, so the change is reviewed in a PR of its own.
- `[ExcludeFromCodeCoverage]` is the last resort after "write the test" and "make the code testable". It is
  allowed on generated code and on a method whose only job is detecting infrastructure.

*Enforced by:* review; the Protected Files Guard for the wide form.

## Formatting

| Rule | Enforced by |
|---|---|
| Allman braces: every brace on its own line | `scripts/format.ps1 -Check` before pushing (`csharp_new_line_before_open_brace = all`); review |
| File-scoped namespaces | `IDE0161` (*error*) |
| Four-space indentation; `var` when the type is apparent | `scripts/format.ps1 -Check` before pushing; review |
| Three blank lines between constructors, methods and properties | review |
| A multi-line argument list opens with `(` on its own line, one argument per line, `)` on its own line | review |
| A method chain puts every call on its own line, starting with the `.`, the receiver alone on the first line | review |
| Public members under `src/` have XML documentation | `CS1591` through `GenerateDocumentationFile` (`Directory.Build.props`) |
| Nullable reference types are on in every project | `Directory.Build.props` |

```csharp
var result = SomeMethod
(
    firstArgument,
    secondArgument
);

var page = await searches
    .Where(s => s.IsActive)
    .OrderBy(s => s.RunOrder)
    .ToListAsync(cancellationToken)
    .ConfigureAwait(false);
```

## Naming

| Rule | Enforced by |
|---|---|
| An async method ends in `Async`; test methods are excluded | `VSTHRD200` (*error*, `src/.editorconfig`) |
| A test is named `MethodUnderTest_when_condition_expected_result` | `TestNamingTests` |
| A conversion is an extension method named for the lower-level type: `ToDto()` / `FromDto()` live in `Business`, `ToEntity()` / `FromEntity()` live in `Data`, and no other project defines either (ADR 0002 §3) | `ConversionMethodPlacementTests` |
| A project is `Wolfgang.Barefoot.<Name>` under `src/`; its tests are `Wolfgang.Barefoot.<Name>.Tests.Unit` under `tests/` | `ProjectLayoutTests` |

## Time

Time is a UTC `DateTimeOffset` everywhere except display. The clock is an injected `TimeProvider`.

| Rule | Enforced by |
|---|---|
| No code reads a static clock: `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow` | `BannedSymbols.txt` |
| `TimeProvider.System` is named only in a host's composition root, inside a `#pragma` that says so | `BannedSymbols.txt`; review of the pragma |
| No public member of a `src/` assembly exposes `DateTime` | `TimeConventionTests` |
| A date with no time of day (a check-in date) is a `DateOnly` | review |
| Time zones are IANA ids resolved with `TimeZoneInfo`; no time-zone library | `DependencyPolicyTests` (NodaTime is on the deny list) |

## Async

| Rule | Enforced by |
|---|---|
| Every `await` under `src/` states its context. `ConfigureAwait(false)` everywhere except a Blazor component, which says `ConfigureAwait(true)` because it must resume on the renderer's context. No project is exempt. | `CA2007` (*error*, `src/.editorconfig`) |
| A test never awaits with `ConfigureAwait(false)`; a bare `await` is correct there | `xUnit1030` (*error*, `tests/.editorconfig`) |
| No blocking on a task: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `Thread.Sleep` | `BannedSymbols.txt` |
| All file and stream I/O is asynchronous | `BannedSymbols.txt` |
| No `async void`; no blocking wait inside async code | `VSTHRD100`, `VSTHRD101`; `VSTHRD002` (*error*) |
| A method that takes a `CancellationToken` passes it on | `CA2016` |

`CA2007` is off under `tests/` because xUnit's rule asks for the opposite there. That is the one place the
two disagree, and inside a test xUnit's rule is the correct one.

## Typed keys, no magic strings

Error codes, permissions and setting keys are small key types in `Wolfgang.Barefoot.Core`: `ErrorCode`,
`Permission`, `SettingKey<T>` (DESIGN §2; ADR 0011, ADR 0012).

| Rule | Enforced by |
|---|---|
| A key is defined once, as a `static readonly` instance in one definitions class | review; the registry rejects a duplicate name at start-up (phase 3) |
| A key's name is a lower-case dotted identifier (`run.lease_seconds`), validated when the key is constructed | unit tests of each key type (phase 1) |
| Every API takes the key type. There is no overload that takes the name as a `string` | review |
| An error code carries its HTTP status and message template; the template is validated when the code is constructed | unit tests of `ErrorCode` (phase 1) |

## Records and classes

| Rule | Enforced by |
|---|---|
| Everything that crosses the API — requests, responses — is a `record` with `init`-only members | `ContractShapeTests` (phase 1) |
| Entities are classes, because EF Core needs to construct and mutate them | review |
| A type used as a test's expected value supports value equality (a `record`) | review |

## Range checks

`IsInRange` and `IsBetween` come from `Wolfgang.Extensions.IComparable`. A range check says which
bounds it includes by the method it calls, not by which of `<` and `<=` it happens to use.

| Rule | Enforced by |
|---|---|
| A value checked against a lower and an upper bound uses `IsInRange` when both bounds are allowed and `IsBetween` when neither is: `status.IsInRange(400, 599)`, never `status is >= 400 and <= 599` or two comparisons joined by `&&` | review |
| A check against one bound, or a range with one bound allowed and the other not, stays a plain comparison | review |

## Regular expressions

Text from the booking site is input nobody here controls (DESIGN §3).

| Rule | Enforced by |
|---|---|
| A `Regex` runs on the non-backtracking engine or carries a timeout | `MA0009` (*error*, `src/.editorconfig`) |
| A pattern that validates a whole value is anchored `\A(?:…)\z`, never `^…$` | review; the parser fixture tests (phase 5) |
| A pattern is built with `RegexOptions.CultureInvariant` | review |
| A text the parser does not recognise is an error for that item, never a guess | the parser fixture tests (phase 5) |

## Logging and console output

| Rule | Enforced by |
|---|---|
| Every host logs through `Microsoft.Extensions.Logging` with structured properties | `BannedSymbols.txt` (`System.Console` is banned) |
| A command-line host writes a verb's own output to the `TextWriter` it was handed; `Program.cs` passes `Console.Out` and `Console.Error` in, inside a `#pragma` that says so | `BannedSymbols.txt`; review of the pragma |
| stdout carries only the verb's output, formatted with the invariant culture | tests of each verb (phase 5) |
| An e-mail address or a digest body is never logged above `Debug` | review |
| A log entry for an API call carries the `traceId` its problem detail reports | API contract tests (phase 4) |

## Layers

The project table and the dependency rules are DESIGN §2. In short: every client reaches the database
through the API; `Core`, `Contracts` and `Presentation` reference no framework.

| Rule | Enforced by |
|---|---|
| `Scraper`, `Admin` and `Web` reference neither `Data` nor an EF Core or database package | `ProjectDependencyTests` |
| `Admin` never references `Obe`; `Maintenance` never references `ApiClient` or `Obe`; `Api` never references `Obe` | `ProjectDependencyTests` |
| `Core`, `Contracts` and `Presentation` reference no framework: no ASP.NET Core, no EF Core, no Playwright, no UI package | `ProjectDependencyTests` |
| `Contracts` references no project; `Core` references only `Contracts` (ADR 0002 §4) | `ProjectDependencyTests` |
| Every project under `src/` is one the design names | `ProjectLayoutTests` |
| Services are registered explicitly; nothing scans an assembly for types to register | review |
| No `IQueryable` crosses a `Core` interface; one query class per read, projecting with `AsNoTracking` | review; `docs/DATABASE-CONVENTIONS.md` (phase 2) |
| Every endpoint declares a permission, or `AllowAnonymous()` with a comment saying why | `EndpointPermissionTests` (phase 4) |
| Every routable page is under the gated layout or carries an `[Authorize]` policy | `PageGateTests` (phase 7) |

## Dependencies

Prefer the base class library and Microsoft packages. Any other package needs a written reason in the PR.
Libraries from `Chris-Wolfgang` are exempt.

| Rule | Enforced by |
|---|---|
| No project references a package on the deny list; every entry on the list carries its reason | `DependencyPolicyTests` |
| Every distributed package's licence is on the allow list | `license-audit.yaml` |

The deny list and its reasons are in `DependencyPolicyTests`, the one place they are written.

## Tests

| Rule | Enforced by |
|---|---|
| A test is named `MethodUnderTest_when_condition_expected_result` | `TestNamingTests` |
| A test asserts something. One that only proves "does not throw" says so: `Assert.Null(Record.Exception(...))` | `S2699` (*error*, `tests/.editorconfig`) |
| A test asserts the behaviour its name promises | review |
| No test reads the real clock or date. A test takes time from the test kit's fake `TimeProvider` | `BannedSymbols.txt` (`RS0030` is on under `tests/`) |
| A test that changes process-wide state — an environment variable, the current directory, the default culture, the exit code, the console streams — is in a collection that does not run in parallel | `TestIsolationTests` |
| Test doubles live in `Wolfgang.Barefoot.TestKit`; no test project defines its own fake clock or fake site client | `TestKitOwnershipTests` |
| Boundary cases the production code handles are exercised | review; Stryker's break threshold |
| Every `.Tests.Unit` project has a `stryker-config.json` that names the project it mutates and sets `thresholds.break` above zero. The floor is raised as the score allows, never lowered | `MutationFloorTests`; `stryker.yaml` fails a PR that scores below the floor |
| Tests use xUnit's built-in assertions; no assertion or mocking library | `DependencyPolicyTests` |
| An existing assertion is not weakened or removed to make a build pass | review; the PR lists such changes under **Test changes (for review)** |

### Coverage

Every `src/` assembly is held to 95 % line coverage and every assembly under `tests/` to 100 %
(`pr.yaml`). The second number means a line of test code that no test runs is dead and gets deleted.

**Test doubles are test code and are held to the same 100 %.** They are not excluded from the gate. They
live in one project, `Wolfgang.Barefoot.TestKit`, which has its own test project; a double's member is
covered either by the kit's tests or by the tests that use the double. A double with a member nothing
calls loses that member. This is what keeps a fake from growing a behaviour the real thing does not have.

## Text and culture

| Rule | Enforced by |
|---|---|
| Money shown to a person is formatted by one formatter that always writes `$`, whatever the host culture | unit tests of the digest renderer (phase 1) |
| Parsing and formatting name their culture; anything a machine reads uses `CultureInfo.InvariantCulture` | `CA1305` (*error*, `src/.editorconfig`); review for the choice of culture |
| String comparison names its `StringComparison` | `CA1307`, `CA1310` (*error*, `src/.editorconfig`) |
| A uniqueness rule checked in memory compares a normalized form, so it agrees with the database's collation | `NameNormalizationTests` (phase 2) |
