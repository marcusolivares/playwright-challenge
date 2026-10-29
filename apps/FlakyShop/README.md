# Flaky Shop lab

A small app with a deliberately flaky test suite, for practising the interview task: find the **root cause** of a
flaky test and fix that, instead of adding retries.

- The app (`apps/FlakyShop`) is an ASP.NET Core app on port **5080**. Its backend is slow at random, and some state is
  shared between all users, much like a real staging environment.
- The tests are in `tests/PlaywrightChallenge.Tests/Ui/FlakyShop/` and the page objects in
  `src/PlaywrightChallenge.Framework/Pages/FlakyShop/`. Every test is flaky, and the bug can be in the test **or** in
  the page object.
- Answers are in [SOLUTIONS.md](SOLUTIONS.md). Don't open it until you've tried the exercises.

## 1. Start the app

```powershell
dotnet run --project apps/FlakyShop          # http://localhost:5080, Ctrl+C to stop
```

Open http://localhost:5080 and click through the pages. The team task list is kept in memory, so restarting the app
clears it.

The app is not part of `PlaywrightChallenge.sln`. While it runs, Windows locks its `.exe`, and building the whole
solution would fail on that lock.

## 2. Run the lab tests

On your machine (a browser window opens, as `.runsettings` sets headed mode):

```powershell
dotnet test --filter "TestCategory=Lab"
dotnet test --filter "FullyQualifiedName~OrdersTests"                    # one class
dotnet test --filter "TestCategory=Lab" -- Playwright.LaunchOptions.Headless=true
```

In Docker, which is the interview setup (run from the repository root in PowerShell):

```powershell
docker run --rm --ipc=host `
  -v "${PWD}:/work" -w /work `
  -v nuget-packages:/root/.nuget/packages `
  -e E2E_FlakyShopUrl=http://host.docker.internal:5080 `
  mcr.microsoft.com/playwright/dotnet:v1.49.0-noble `
  dotnet test tests/PlaywrightChallenge.Tests --filter "TestCategory=Lab" -- Playwright.LaunchOptions.Headless=true
```

| Part | Why |
|---|---|
| `-v "${PWD}:/work" -w /work` | Mounts the repository into the container, so traces are written to your disk |
| `-v nuget-packages:...` | A named volume that keeps NuGet packages between runs, so later runs restore faster |
| `--ipc=host` | Chromium needs more shared memory than Docker gives a container by default |
| `host.docker.internal` | Inside the container, `localhost` is the container itself. This name points to your machine |
| `Headless=true` | The container has no display |

## 3. The exercises

| Test | Page | Symptom |
|---|---|---|
| `OrdersTests.Loading_orders_lists_all_five` | Orders | Fails about half the time, "Expected 5 but was 0" |
| `CatalogTests.Adding_a_product_puts_it_in_the_cart` | Catalog | Fails about half the time, wrong item in the cart |
| `TasksTests.New_task_appears_in_the_list` | Team tasks | Passes on a fresh app, then fails every run |
| `TasksTests.Completing_a_task_marks_it_done` | Team tasks | Passes alone, fails after other tests have run |
| `CheckoutTests.Placing_an_order_confirms_it` | Checkout | Fails about half the time, confirmation never appears |
| `SearchTests.Search_shows_only_matching_products` | Search | Fails more often when the server is **fast** |

For each one, practise saying your reasoning out loud:

1. **Reproduce it.** Run it several times and note the failure rate. Then run it alone versus with the rest of the
   suite, and on a freshly restarted app versus one that has been used.
2. **Gather evidence.** Read the error and open the trace. Don't guess.
3. **State a hypothesis**, e.g. "the assertion runs before the response arrives".
4. **Fix the cause.** Don't add `[Retry]`, longer timeouts or sleeps.
5. **Prove the fix** by running the test 10+ times and getting the same result every time.

Run a test repeatedly from PowerShell:

```powershell
dotnet build
1..10 | % { dotnet test --no-build --filter "FullyQualifiedName~OrdersTests" -- Playwright.LaunchOptions.Headless=true | Select-String "Passed!|Failed!" }
```

Alternatively, put `[Repeat(20)]` on the test temporarily. NUnit runs it 20 times and stops at the first failure.

## 4. Reading a trace

`UiTestBase` records a trace for every test and keeps it only when the test fails:

```
tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright-artifacts/traces/<Full.Test.Name>.zip
tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright-artifacts/screenshots/<Full.Test.Name>.png
```

Open a trace in either of two ways:

```powershell
pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 show-trace "tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright-artifacts/traces/PlaywrightChallenge.Tests.Ui.FlakyShop.OrdersTests.Loading_orders_lists_all_five.zip"
```

or drag the `.zip` onto https://trace.playwright.dev. That page processes the file in your browser and doesn't upload
it anywhere.

Traces from a Docker run end up in the same folder, because the repository is mounted. Open them on your machine,
since the container has no display.
