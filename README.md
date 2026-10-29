# Playwright Challenge

A minimal C# + [Playwright for .NET](https://playwright.dev/dotnet/) test suite (NUnit, .NET 8, Playwright 1.49), trimmed down from the `playwright-dotnet` blueprint. It has one example test class that runs against the public [Practice Software Testing](https://practicesoftwaretesting.com) demo app.

## Layout

```
.
├── PlaywrightChallenge.sln
├── Directory.Build.props        # shared build settings (net8.0, nullable, warnings-as-errors on CI)
├── Directory.Packages.props     # every NuGet version, in one place
├── apps/FlakyShop/              # local practice app with a deliberately flaky suite (not in the solution)
├── src/PlaywrightChallenge.Framework/      # how to talk to the app (no NUnit, no asserts)
│   ├── Configuration/  TestSettings + layered loader (appsettings*.json, E2E_* env vars)
│   ├── Constants/      Routes and UI copy
│   ├── Pages/          BasePage + one page object per screen
│   └── Utilities/      small helpers (UniqueId)
└── tests/PlaywrightChallenge.Tests/        # what to test
    ├── Infrastructure/ UiTestBase (fresh context per test, trace/screenshot on failure)
    ├── Ui/             tests
    ├── GlobalSetup.cs  loads settings once per run
    ├── appsettings.json
    └── .runsettings    browser, headless, workers, expect timeout
```

## Getting started

```bash
dotnet build
pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 install   # browsers, once per Playwright version
dotnet test
```

## Running and debugging

```bash
dotnet test --filter "FullyQualifiedName~ProductSearchTests"   # one class
dotnet test --filter "TestCategory=Smoke"                      # by category
dotnet test -- Playwright.BrowserName=firefox                  # chromium | firefox | webkit
dotnet test -- Playwright.LaunchOptions.Headless=true          # .runsettings shows the browser by default
```

- **Breakpoints:** open the solution in VS Code (C# Dev Kit) or Visual Studio, set a breakpoint in a test or page object, and use "Debug Test" from the Test Explorer. The browser is headed by default, so you can watch each step.
- **Playwright Inspector:** `$env:PWDEBUG=1; dotnet test` pauses on every action and lets you step through.
- **Record locators:** `pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 codegen https://practicesoftwaretesting.com`
- **Failures:** a trace and a screenshot are written to `tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright-artifacts/`. Open a trace with `playwright.ps1 show-trace <file.zip>` or at [trace.playwright.dev](https://trace.playwright.dev).

## Docker

The interview image `mcr.microsoft.com/playwright/dotnet:v1.49.0-noble` ships the .NET 8 SDK, PowerShell and the
Playwright 1.49 browsers, which is why the suite targets `net8.0` and Playwright 1.49.0. Keep the NuGet version in step
with the image tag. A newer package looks for browser builds the image doesn't have.

```powershell
docker run --rm --ipc=host -v "${PWD}:/work" -w /work mcr.microsoft.com/playwright/dotnet:v1.49.0-noble `
  dotnet test --filter "FullyQualifiedName~ProductSearchTests" -- Playwright.LaunchOptions.Headless=true
```

## Flaky test lab

`apps/FlakyShop` is a local app on port 5070 with one deliberately flaky test per root cause: waits, selectors, shared
test data, and two race conditions. Its tests carry the `Lab` category, and CI excludes them. See
[apps/FlakyShop/README.md](apps/FlakyShop/README.md).

## Pointing it at your app

1. Set `BaseUrl` and `TestIdAttribute` in `tests/PlaywrightChallenge.Tests/appsettings.json` (or in a git-ignored `appsettings.local.json`, or via `E2E_BaseUrl`).
2. Add a page object per screen under `src/PlaywrightChallenge.Framework/Pages/`, deriving from `BasePage`.
3. Add test classes under `tests/PlaywrightChallenge.Tests/Ui/`, inheriting from `UiTestBase`.
4. Replace `HomePage`, `ProductSearchTests`, `Routes` and `UiTexts` once you no longer need the example.

Need API clients, sign-in reuse, test-data factories or a database layer? The `playwright-dotnet` blueprint has them; copy the pieces over as you need them.
