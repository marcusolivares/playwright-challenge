---
name: playwright-trace-dotnet
description: Read the Playwright traces this .NET suite saves for failed tests (tests/PlaywrightChallenge.Tests/bin/Debug/net*/playwright-artifacts/traces/*.zip) from the command line - actions with C# source lines, errors, network timing, console, and the page's DOM before and after any step. Use when a UI test failed or is flaky and you need evidence of what happened, without opening a browser.
allowed-tools: Bash(npx --yes playwright@1.64.1 trace:*)
---

# Reading the suite's Playwright traces

`UiTestBase` records a trace for every test and keeps it only when the test fails:

```
tests/PlaywrightChallenge.Tests/bin/Debug/net*/playwright-artifacts/traces/<Namespace.Class.Test>.zip
tests/PlaywrightChallenge.Tests/bin/Debug/net*/playwright-artifacts/screenshots/<Namespace.Class.Test>.png
```

The repo's NuGet Playwright has no trace command line, so use the npm one. It reads traces from older Playwright versions. Run it from the repository root:

```bash
npx --yes playwright@1.64.1 trace <command>
```

`trace open` extracts into `.playwright-cli/trace` (git-ignored). Every later command works on the open trace until `trace close` or the next `trace open`.

## Workflow

1. `trace open <zip>`: browser, duration, and counts of actions, requests, errors and console messages.
2. `trace actions`: every step with its id, start time and duration. `--errors-only` shows the failed ones, `--grep <regex>` filters by title.
3. `trace errors`: the failure messages with C# source locations such as `BasePage.cs:16:42`.
4. `trace action <id>`: parameters (the resolved selector), the retry log, the error, the C# call stack, and which DOM snapshots exist.
5. `trace snapshot <id> --phase before|after`: the accessibility tree of the page at that moment. Add `-- eval "<js>"` to query that frozen DOM, or `-- screenshot --filename=<file>.png` to render it.
6. `trace requests`: every request with start time, status and duration, on the same clock as `trace actions`. `--failed`, `--grep <regex>` and `--method POST` filter it; `trace request <id>` shows headers and body.
7. `trace console --errors-only`: browser console errors.
8. `trace close` when done.

## What to look for in this suite

- **Order of events.** Compare when an action or assertion ran with when the request it depends on started and finished. A read or click that happens before the response it needs is a race.
- **One-time reads.** `Locator.CountAsync`, `Locator.AllInnerTextsAsync` and similar appear as single quick actions without retries. A web-first assertion shows its retries in the `Log` of `trace action <id>`.
- **What the page showed.** Snapshot the step before the failure, and the step that read or clicked something, to see what the page really contained at that moment. Check the final snapshot too: a test can pass while the screen ends up wrong.
- **Fixed waits.** `Page.WaitForTimeoutAsync` shows up as an action with exactly its duration.
- **Comparing runs.** Each failure overwrites the trace for that test. Copy a trace aside before re-running when you want to compare a failing run with another, for example to `.playwright-cli/<test>-run3.zip`.
- Action titles are .NET API names (`Page.GotoAsync`, `Locator.ClickAsync`, `LocatorAssertions...`), and source locations point at the test or page object line.

## Troubleshooting

- `trace snapshot` needs the Chromium build of Playwright 1.64.1. If it says a browser is missing, run `npx --yes playwright@1.64.1 install chromium-headless-shell`.
- In Git Bash, quote Windows paths or use forward slashes, and prefix commands whose arguments start with `/` with `MSYS_NO_PATHCONV=1`.
