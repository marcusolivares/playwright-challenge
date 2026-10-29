---
name: flaky-test-triage
description: Method for finding and fixing the root cause of a flaky or intermittently failing Playwright test in this .NET suite - reproduce and measure the failure rate, gather evidence from the error, the trace and the app code, state and test a hypothesis, fix the cause without retries, sleeps or longer timeouts, and prove the fix with repeated runs. Use when a test fails only sometimes, only with other tests, only on a used environment, or when asked to fix the Flaky Shop Lab tests.
---

# Flaky test triage

Find the root cause and fix that. A fix counts only when repeated runs prove it.

Never "fix" by adding `[Retry]`, sleeps (`Thread.Sleep`, `Task.Delay`, `WaitForTimeoutAsync`), longer timeouts or `NetworkIdle` waits, and never by deleting, skipping or weakening the test. Retries hide product bugs and keep failing at the same rate per attempt: a test that fails half the time still fails all three attempts one time in eight. In this suite, NUnit's `[Retry]` doesn't even retry Playwright's `Expect` failures or timeouts.

Don't read or search `apps/FlakyShop/SOLUTIONS.md`: it is the lab's answer key. Exclude it from every search (`glob: "!**/SOLUTIONS.md"`, `grep --exclude=SOLUTIONS.md`) and work from evidence.

## 1. Reproduce and measure

Build, then run the test repeatedly. This loop keeps each run's log and copies every failing trace, because the next failure overwrites it:

```bash
t=OrdersTests.Loading_orders_lists_all_five      # Class.Test
dotnet build tests/PlaywrightChallenge.Tests && mkdir -p .playwright-cli/runs
for i in $(seq 1 10); do
  log=.playwright-cli/runs/$t-$i.log
  dotnet test tests/PlaywrightChallenge.Tests --no-build --filter "FullyQualifiedName~$t" -- Playwright.LaunchOptions.Headless=true > $log 2>&1
  if grep -q "Failed!" $log; then
    cp tests/PlaywrightChallenge.Tests/bin/Debug/net*/playwright-artifacts/traces/*.$t.zip .playwright-cli/runs/$t-$i.zip
    echo "run $i failed: $(grep -m1 -A3 'Error Message:' $log | tail -n +2 | tr -s ' ' | tr '\n' ' ')"
  elif grep -q "Passed!" $log; then echo "run $i passed"
  else echo "run $i: no result, check $log"; fi
done
```

- Prefix the `dotnet test` line with `E2E_FlakyShopUrl=http://localhost:<port>` when you use your own Flaky Shop instance.
- Ten runs is enough for a test that fails about half the time; use more for rarer failures. A temporary `[Repeat(20)]` on the test also works: NUnit stops at the first failure. Remove it afterwards.
- Change one condition at a time and compare failure rates:
  - the test alone versus its whole class or category, which exposes order dependence and shared data;
  - a freshly restarted app versus one that has been used, which exposes leftover server state (`flakyshop.sh restart <port>` before each run, see the dotnet-ui-test skill);
  - fast versus slow server responses, which exposes races.
- In a class or category run, the summary counts every test, and the other Lab tests fail on purpose. Read only this test's outcome:

```bash
dotnet test tests/PlaywrightChallenge.Tests --no-build --filter "TestCategory=Lab" --logger "console;verbosity=normal" -- Playwright.LaunchOptions.Headless=true 2>&1 | grep -E "^\s+(Passed|Failed) Loading_orders_lists_all_five"
```

## 2. Gather evidence

- **The error** from the `dotnet test` output: expected versus actual values, timeouts, strict-mode violations ("resolved to N elements").
- **The trace** (playwright-trace-dotnet skill): which step failed or fed the failing assertion, what ran just before it, when the requests it depends on started and finished compared with that step, and what the DOM held at each point. Compare several failing traces.
- **Passing runs keep no trace.** To see what a passing run does, repeat the test's steps in your own playwright-cli session with `run-code --filename=.playwright-cli/probe.js`, logging when the requests the step depends on start and finish (`page.on('request')`, `page.on('response')`) and what the page holds at the moment the test reads or clicks. Run the probe several times.
- **The app code** when you have it. For the Flaky Shop that is `apps/FlakyShop/wwwroot/<page>.html` and its endpoints in `apps/FlakyShop/Program.cs`: when data loads, when handlers attach, how long responses can take, what state is shared.
- **The test and page object code:** for every line, what does it actually wait for, and what does it assume about order, timing or data?
- The failure screenshot, when the trace isn't enough.

## 3. State a hypothesis and test it

Write one sentence that names the cause and the mechanism, for example "the list is read once, before the response that fills it arrives". Predict what you must see if it's right, such as the read happening before the response in failing runs and after it in passing ones, and check that prediction before changing any code.

You can't set the Flaky Shop's speed, so make the suspected condition certain instead of waiting for it: for example, make the step wait for the earlier load before acting, or delay one response with `page.route` in your probe. The failure rate should go to 0% or 100%, as the hypothesis predicts. Revert such experiments before you fix anything. If the evidence disagrees, form a new hypothesis.

## 4. Fix the cause

Fix where the cause lives: readiness, waits and locators in the page object; data and assertions in the test.

| Symptom | Typical root cause | Fix |
|---|---|---|
| A value is read too early: zero items, empty text, a partial list | A one-time read (`CountAsync`, `AllInnerTextsAsync`, `TextContentAsync`, `IsVisibleAsync`), often after a fixed wait | A web-first assertion that retries until the expected state appears |
| The wrong item is clicked or checked, depending on order | A locator based on position (`nth-child`, `Nth`, an index) in a list whose order changes | Locate the item by what it is: filter by a child or text that identifies it, with `Exact = true` if names overlap |
| Passes alone or on a fresh app, fails after other tests | Shared state: reused data names, assertions on the whole shared list, order dependence, per-test values stored in fields that NUnit shares across tests | Unique data per test (`UniqueId.Next()` in a local), assert only on the records the test created |
| An action succeeds but nothing happens | Acting before the page is really ready, for example before handlers are attached or data has loaded; visible and enabled doesn't mean ready | Wait for the real readiness signal, ideally in the page object's `ReadyIndicator` or action, and report the product bug |
| Wrong or stale results that depend on server speed | Responses racing each other, or an assertion that matched an earlier state of the page | Let the earlier load finish before acting, and assert the exact final state with one retrying assertion; report the race |
| A timeout under load | Waiting for the wrong thing | Find what the step depends on and wait for that; a longer timeout only moves the failure |

Keep the fix minimal, and remove the retries, sleeps or fixed waits the test already had once the real wait is in place. Make code changes with the Edit tool, and rebuild after every change: the loops use `--no-build`.

## 5. Prove it

- Run the fixed test at least 10 times in each condition that failed before, and a few times in the others: alone, with its category, on a used app, and on a fresh app (restart before each of those runs). Every run must pass.
- Prove it can still fail: temporarily break the expectation, for example expect a value the app never shows, rebuild, run once and confirm it fails on that assertion. Then restore the line exactly and rebuild.
- Format only the files you changed, `dotnet format --include <files>`, then confirm with `dotnet format --verify-no-changes` and `CI=true dotnet build tests/PlaywrightChallenge.Tests`.

## 6. Report

For each test, in this order:

1. **Reproduction:** runs, failures and the conditions that changed the rate.
2. **Evidence:** the error text and the trace facts, with action ids, timings and snapshot excerpts, plus probe results.
3. **Root cause:** in the test, the page object or the app, and the mechanism.
4. **Fix:** what changed and why it removes the cause, not just the symptom.
5. **Proof:** passes out of runs after the fix, in which conditions, and the result of the negative check.
6. **For the developers:** product bugs the investigation exposed, such as a button that ignores early clicks or responses that overwrite newer ones, and coverage the fixed test no longer has.
