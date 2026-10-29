---
name: dotnet-ui-test
description: How to write, change and run this repository's C# Playwright UI tests (NUnit, Playwright for .NET, page objects in src/PlaywrightChallenge.Framework). Use when adding or editing page objects or tests, turning playwright-cli output or a codegen recording into C#, running dotnet test, or starting the Flaky Shop app that the Lab tests need.
---

# Writing and running UI tests in this repo

The suite is C# with NUnit and Playwright for .NET. It is not a Node project:

- Never run `npx playwright test` or `npm init playwright`, and never create `*.spec.ts`, `playwright.config.ts` or `package.json`. The playwright-cli skill's test-running and test-generation references describe the Node test runner; here, use the commands below.
- Use playwright-cli only to explore pages and collect locators and expected values. `.playwright/cli.config.json` makes it resolve `GetByTestId` against `data-test`, the app's attribute (keep it in sync with `TestIdAttribute` in appsettings.json), and makes action commands print C#.
- Invoke the CLI as `playwright-cli`. If the shell can't find it, use `npx --yes @playwright/cli@0.1.22` with the same arguments. The playwright-cli skill's Installation section suggests a global install; in this repo, use that npx form instead.

## playwright-cli in this repo

- Run it from the repository root so it finds `.playwright/cli.config.json`. Its snapshots and logs go to `.playwright-cli/`, which is git-ignored; put throwaway exploration scripts there too.
- Match the suite's viewport first: `resize 1920 1080` (`TestSettings.Viewport`).
- Action commands (`goto`, `click`, `fill`, `select`) print C#. `generate-locator`, `resize` and `reload` still print JavaScript, so translate those by hand.
- `select` accepts an option's visible label as well as its value.
- The CLI returns before the app's data requests finish. After an action that reloads data, confirm a completion signal or a changed value before you record a result.
- `requests` hides static files unless you pass `--static`; `request <n>`, `request-body <n>` and `response-body <n>` show one request, which matters when the app sends its parameters in a request body.
- Keep an `eval` expression on one line; a multi-line function fails with a syntax error. `--raw` prints the result as JSON, so a string comes back quoted.
- In Git Bash, an argument that starts with `/` is rewritten into a Windows path, so `find --regex "/sort/i"` silently finds nothing. Use a pattern without slashes (`find --regex "[Ss]ort"`) or prefix the command with `MSYS_NO_PATHCONV=1`.

## Where things go

| What | Where |
|---|---|
| Page objects, no NUnit and no asserts | `src/PlaywrightChallenge.Framework/Pages/`, Flaky Shop ones in `Pages/FlakyShop/` |
| Routes and UI copy | `src/PlaywrightChallenge.Framework/Constants/Routes.cs` and `UiTexts.cs` |
| Tests | `tests/PlaywrightChallenge.Tests/Ui/`, Flaky Shop ones in `Ui/FlakyShop/` |
| Base classes | `UiTestBase` for the public demo site (`BaseUrl`); `FlakyShopTestBase` for the local Flaky Shop (`FlakyShopUrl`, adds category `Lab`) |
| Settings | `tests/PlaywrightChallenge.Tests/appsettings.json`, then `appsettings.local.json`, then `E2E_*` environment variables |
| Failure artifacts | `tests/PlaywrightChallenge.Tests/bin/Debug/net*/playwright-artifacts/traces/<Full.Test.Name>.zip` and `screenshots/<Full.Test.Name>.png` |

## Page objects

Copy the shape of the existing ones (`HomePage.cs`, `Pages/FlakyShop/*.cs`):

- `public sealed class XPage(IPage page) : BasePage(page)`, one per screen.
- Locators tests assert on are public get-only properties initialized from `page`. Locators only actions use are `private readonly ILocator _name` fields. Locators that take a parameter are methods returning `ILocator`.
- `ReadyIndicator` must prove the page is usable for the next action, not merely painted. If the page loads data or wires up handlers asynchronously, point it at what appears once that is done.
- Navigation goes through `NavigateAsync(Routes.X)`; add new paths to `Routes`.
- Actions are user-level `...Async` methods that return only once the app has finished reacting: wait for a completion element, a state change or the response that matters (`Page.RunAndWaitForResponseAsync`). Never sleep.
- When one URL serves several requests, such as the GET and POST of one path or a page-load fetch still in flight, wait for the response with a predicate on method and URL, not a glob that any of them can satisfy.
- In methods, use the `Page` property from `BasePage`. Use the primary-constructor parameter `page` only in field and property initializers: capturing it in a method body raises warning CS9107, which fails the CI build.

## Locators

Priority: role or label (`GetByRole`, `GetByLabel`) when the element has a distinguishing accessible name, then test id (`GetByTestId`, also the right choice for unnamed regions, rows and cells), then CSS as a last resort.

- Identify an element by what it is, never by its position. No `nth-child`, `Nth(i)` or index arithmetic on lists whose order can change; filter the item locator by a child or text that identifies it (`Filter(new() { Has = ... })` or `HasText`).
- Use `Exact = true` when one accessible name can be a substring of another.
- Reject generated locators that embed data, such as database ids, GUIDs, prices or dates (for example `GetByTestId("product-01M628ETNB63Z94E453EKZRFR2")`). Use the shared test id plus a filter.
- Translate CLI output into page object members: `await page.GetByTestId("search-query").FillAsync("pliers");` becomes `public ILocator SearchInput { get; } = page.GetByTestId("search-query");` plus an action that uses it. Literal URLs become `Routes` constants; asserted UI copy becomes `UiTexts` constants.
- A codegen recording needs more than translation: its locators often embed data, and it has no waits or assertions. Follow [references/codegen.md](references/codegen.md).

## Assertions

- UI state: web-first `await Expect(locator)...`, which retries until `ExpectTimeout` (`.runsettings`). Plain values: `Assert.That`.
- `CountAsync()`, `IsVisibleAsync()`, `TextContentAsync()`, `InnerTextAsync()` and `AllInnerTextsAsync()` read the page once and never retry. Don't assert on their result while the UI may still change; use `ToHaveCountAsync`, `ToHaveTextAsync` (a string array checks a whole list), `ToContainTextAsync`, `ToHaveValueAsync`, `ToBeVisibleAsync` or `ToMatchAriaSnapshotAsync` instead. Read values once only after a web-first assertion has proven the final state, for example to check an ordering with `Assert.That`.
- Assert the outcome the user sees: the filtered names, the cart line, the confirmation text. A URL change, "some item is visible" or a count above zero is never enough on its own.
- Prefer one assertion that waits for the exact final state over a visibility check followed by a one-time read.
- A single-string `ToHaveTextAsync` or `ToContainTextAsync` needs exactly one matching element. On a locator that matches a list it becomes a strict-mode violation as soon as a second item exists; filter to the item, or pass an array.
- Ask what bug would make the test fail. If nothing realistic comes to mind, the assertion is too weak.

## Waiting

Forbidden as fixes or waits: `Thread.Sleep`, `Task.Delay`, `WaitForTimeoutAsync`, `NetworkIdle`, `[Retry]` and raised timeouts. A hook blocks them in the test-generator, test-from-codegen and test-healer subagents. Wait for the real condition instead: a web-first assertion, the page's ready indicator, a completion element, or the response the next step depends on.

## Test data and isolation

- Every test gets a fresh browser context, but server state can be shared. The Flaky Shop keeps one task list for everybody until it restarts, like a shared staging environment.
- Create uniquely named data in each test (`UniqueId.Next()`) and assert only on the records that test created, never on whole-list counts of shared data.
- NUnit creates one instance of a test class for all its tests. Keep per-test values in locals or assign them in `[SetUp]`, never in constants or field initializers.
- Tests must not depend on order (NUnit runs them alphabetically) or on what other tests leave behind.

## Tests

- `public sealed class XTests : UiTestBase` with `[Category(TestCategories.Ui)]`, or `: FlakyShopTestBase` for the Flaky Shop. Add `[Category(TestCategories.Smoke)]` to smoke tests.
- `[SetUp]` creates the page object and navigates; tests act only through page objects.
- Names are sentences: `Search_without_matches_says_so`.
- A test generated from a plan carries a `// Plan: specs/<file>.plan.md <id>` comment above `[Test]`.
- Templates and API idioms: [references/templates.md](references/templates.md).

## Building and running

```bash
dotnet build tests/PlaywrightChallenge.Tests      # the test project is all dotnet test needs
dotnet test tests/PlaywrightChallenge.Tests --no-build --filter "FullyQualifiedName~ProductSearchTests.Search_without_matches_says_so" -- Playwright.LaunchOptions.Headless=true
dotnet test tests/PlaywrightChallenge.Tests --no-build --filter "TestCategory=Lab" -- Playwright.LaunchOptions.Headless=true
dotnet format                                      # CI runs it with --verify-no-changes
CI=true dotnet build tests/PlaywrightChallenge.Tests   # turns warnings into errors, as CI does
```

- Always pass `-- Playwright.LaunchOptions.Headless=true`; `.runsettings` opens a visible browser for humans.
- `--filter "FullyQualifiedName~Name"` is a substring match: `~OrdersTests` runs the whole class.
- A failed test leaves its trace and screenshot under `playwright-artifacts/`; read the trace with the playwright-trace-dotnet skill. Each failing run overwrites the previous files for that test.
- CI excludes the Lab tests (`--filter "TestCategory!=Lab"`). The interview setup runs them in the `mcr.microsoft.com/playwright/dotnet:v1.49.0-noble` image; the command is in `apps/FlakyShop/README.md`.

## The Flaky Shop app

Lab tests need `apps/FlakyShop` running at `FlakyShopUrl` (default `http://localhost:5080`). Manage it with:

```bash
bash .claude/skills/dotnet-ui-test/scripts/flakyshop.sh status    # or: start | stop | restart, optional port as 2nd argument
```

- `restart` gives a fresh app, which clears the shared task list. It stops whatever listens on that port, including an instance someone started by hand; say so when you do it.
- On another port: `flakyshop.sh start 5081`, then run tests with `E2E_FlakyShopUrl=http://localhost:5081 dotnet test ...`.
- Its backend is slow at random and some endpoints change their results between calls, so one passing run proves nothing.
