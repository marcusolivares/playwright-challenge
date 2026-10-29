# CLAUDE.md

Guidance for Claude Code in this repository: a minimal C# + Playwright for .NET test suite (NUnit, .NET 8, Playwright 1.49), trimmed from the `playwright-dotnet` blueprint.

## Commands

```bash
dotnet build
pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 install   # browsers, once
dotnet test
dotnet test --filter "FullyQualifiedName~ProductSearchTests"
dotnet test -- Playwright.BrowserName=firefox Playwright.LaunchOptions.Headless=true
dotnet format                                                                   # CI runs it with --verify-no-changes
pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 codegen --target csharp-nunit --test-id-attribute data-test --viewport-size "1920,1080" <url>   # record a flow for test-from-codegen
```

## Where code goes

- `src/PlaywrightChallenge.Framework`: everything that talks to the app. Never reference NUnit here and never assert here.
  - `Pages/`: one page object per screen, deriving from `BasePage`. Locators are get-only properties initialized from the primary-constructor `page`; actions are `...Async` methods; navigation goes through `NavigateAsync(route)`.
  - `Constants/`: `Routes` and `UiTexts`. No selectors there; selectors belong to page objects.
- `tests/PlaywrightChallenge.Tests`: tests under `Ui/`, inheriting from `UiTestBase`; lifecycle code in `Infrastructure/`.
- `apps/FlakyShop`: a local practice app (port 5080), kept out of the solution so a running instance never locks the build. Its tests (`Ui/FlakyShop/`, category `Lab`) and page objects (`Pages/FlakyShop/`) are **deliberately flaky**; don't fix them unless asked. Answers are in `apps/FlakyShop/SOLUTIONS.md`.
- Versions are pinned to the interview Docker image (`playwright/dotnet:v1.49.0-noble`: .NET 8 SDK, Playwright 1.49.0). Don't upgrade them.

## Test conventions

- Act via page objects, assert with `Expect(...)` (UI) or `Assert.That` (values).
- Locator priority: role and label, then test id (`GetByTestId`, attribute from `TestIdAttribute`), then CSS as a last resort.
- No `Thread.Sleep` or `WaitForTimeoutAsync`; rely on auto-waiting and web-first assertions.
- Test names are sentences: `Search_without_matches_says_so`.

## Configuration

`appsettings.json`, then `appsettings.{E2E_ENVIRONMENT}.json`, then `appsettings.local.json` (git-ignored), then `E2E_*` env vars. Add new settings as typed properties on `TestSettings`. Runner options (browser, headless, workers, expect timeout) are in `.runsettings`.

## AI agents and skills

Subagents in `.claude/agents/` cover the UI-test workflow:

1. `test-planner` explores the app with playwright-cli and writes `specs/<feature>.plan.md` and `specs/app-map.md`. Review the plan before generating.
2. `test-generator` implements one planned scenario as page-object changes plus an NUnit test, proven by repeated runs and a negative check.
3. `test-reviewer` is read-only: it checks that each assertion proves its outcome and flags flaky patterns.
4. `test-healer` fixes failing or flaky tests at their root cause, using the saved traces.
5. `test-from-codegen` is a second way in: it turns a codegen recording into page-object members and tests, replaying it live to replace brittle locators and infer assertions, and lists the inferred assertions for review. Run `test-reviewer` on its output too.

Skills in `.claude/skills/`: `dotnet-ui-test` (conventions, commands, codegen guide, Flaky Shop helper script), `flaky-test-triage` (reproduce, gather evidence, hypothesize, fix, prove), `playwright-trace-dotnet` (trace command line) and `playwright-cli` (the official browser CLI skill). `.claude/hooks/guard-test-edits.mjs` stops the generator, codegen converter and healer from adding sleeps, fixed waits, `[Retry]`, `NetworkIdle` waits or longer timeouts.

- Run tests only with `dotnet test`. The playwright-cli skill's Node instructions (`npx playwright test`, `--debug=cli`, seed tests, `npm init playwright`) don't apply to this .NET suite.
- Browser CLI: `playwright-cli`, or `npx --yes @playwright/cli@0.1.22` when it isn't on PATH. `.playwright/cli.config.json` makes it print C# and target `data-test` ids; keep that in sync with `TestIdAttribute`.
- Traces: `npx --yes playwright@1.64.1 trace ...`. The pinned NuGet Playwright has no trace command line, and this tool only reads traces; it doesn't change the suite's version.
- In Git Bash, arguments that start with `/`, such as `--regex "/x/i"`, are rewritten into Windows paths; prefix those commands with `MSYS_NO_PATHCONV=1`.
