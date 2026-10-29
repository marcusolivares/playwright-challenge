# Turning a codegen recording into page objects and tests

A codegen recording shows what someone clicked, not what should happen. Its locators are whatever codegen found unique at that moment; it records no waits and, unless someone used the assert tools, no assertions. Use it to learn the intent, then check every locator and expected value against the live app with playwright-cli.

## Recording

From PowerShell, after `dotnet build`:

```powershell
pwsh tests/PlaywrightChallenge.Tests/bin/Debug/net8.0/playwright.ps1 codegen --target csharp-nunit --test-id-attribute data-test --viewport-size "1920,1080" https://practicesoftwaretesting.com
```

- `--test-id-attribute data-test` makes codegen write `GetByTestId("x")` instead of `Locator("[data-test=\"x\"]")`. Keep it in sync with `TestIdAttribute` in appsettings.json.
- `--viewport-size` matches `TestSettings.Viewport`, so the recording sees the same layout as the tests.
- Record what you expect with the toolbar's assert tools (visibility, text, value, snapshot). They become `Expect(...)` lines, and those are kept as stated intent.
- Optional: `--save-trace recording.zip` keeps the DOM of every step, for flows that are hard to replay.

## From recording to repo structure

| Codegen writes | In this repo |
|---|---|
| `public class Tests : PageTest`, `[TestFixture]`, `[Parallelizable(ParallelScope.Self)]` | `public sealed class XTests : UiTestBase` with `[Category(TestCategories.Ui)]` (`FlakyShopTestBase` for the Flaky Shop). Parallelism comes from `.runsettings`. |
| One `MyTest` with every action | One test per user goal, each starting from navigation and named after its outcome. |
| `await Page.GotoAsync("https://host/path")` | A `Routes` constant relative to `BaseUrl` (or `FlakyShopUrl`), opened by the page object's `GotoAsync()`. Any other host needs a setting. |
| `await Page.Locator(...).ClickAsync()` inline | A locator member on the page object, plus a user-level `...Async` action that uses it. |
| `FillAsync(...)` followed by a click or `PressAsync("Enter")` | One action, such as `SearchAsync(term)`. |
| Nothing between two actions | A wait inside the action for what proves the app has finished: a completion element, a state change or the response (`Page.RunAndWaitForResponseAsync`). |
| `await Expect(...)` recorded with the assert tools | Kept in the test, with its locator moved to the page object; a visibility check is strengthened to the content when the content can be checked. |
| `var page1 = await Page.RunAndWaitForPopupAsync(...)`, then `Page1.` or `page1.` | A page-object action that returns the popup's page object (below). Codegen's `page1`/`Page1` mismatch doesn't compile anyway. |
| `CheckAsync()` then `UncheckAsync()` on the same element, hovers, right-clicks (`Button = MouseButton.Right`) | Usually exploration noise: keep them only when they have an effect a test can assert. |

## Locators

Codegen picks a locator that is unique now, not one that stays right. Upgrade it by the priority in SKILL.md:

| Recorded | Problem | Use instead |
|---|---|---|
| `Locator("[data-test=\"search-query\"]")` | CSS for a test id | `GetByTestId("search-query")`, or role and name if the element has a distinguishing accessible name |
| `Locator("[data-test=\"category-01M62FAQH5XXSZ3DT67FYY7CAA\"]")`, `Locator("[data-test=\"product-25\"]")` | A database id: it changes when the data is reseeded | The role or label with its accessible name (`GetByRole(AriaRole.Checkbox, new() { Name = "Hand Tools" })`), or the shared test id filtered by a child (`GetByTestId("product-name")` plus `Filter`) |
| `GetByText("page 3")` on a button or link | A text match, possibly partial, that ignores the role | `GetByRole(AriaRole.Link, new() { Name = "...", Exact = true })` with the name the snapshot shows |
| `.Nth(2)`, `.First` to pick an item | Position in a list whose order can change | `Filter(new() { HasText = ... })` or `Filter(new() { Has = ... })` |
| `GetByRole(AriaRole.Button, new() { Name = "Add to cart" })` | Usually fine | Keep it, with `Exact = true` if another name contains it |

The example names above are illustrative; take the real roles and names from a `snapshot` of the live page.

## Popups and new tabs

A popup is a new `IPage` in the same browser context, so the action that opens it returns the new page's page object, ready for use:

```csharp
/// <summary>Opens the guide in a new tab and returns it once it is ready.</summary>
public async Task<GuidePage> OpenGuideAsync()
{
    var popup = await Page.RunAndWaitForPopupAsync(() => _guideLink.ClickAsync());
    var guidePage = new GuidePage(popup);
    await guidePage.WaitUntilReadyAsync();
    return guidePage;
}
```

The test then acts and asserts through the returned page object. If the popup opens another host, the test can still follow it; only a test that has to start on that host needs a new setting.

## Worked example

Recorded:

```csharp
[Test]
public async Task MyTest()
{
    await Page.GotoAsync("https://practicesoftwaretesting.com/");
    await Page.Locator("[data-test=\"search-query\"]").ClickAsync();
    await Page.Locator("[data-test=\"search-query\"]").FillAsync("pliers");
    await Page.Locator("[data-test=\"search-submit\"]").ClickAsync();
}
```

In this repo, the click into the input is dropped (filling focuses it), the fill and submit become one action that waits until the results are rendered, and the test asserts what the user searched for: that every listed name matches the term.

```csharp
// HomePage.cs
private readonly ILocator _searchButton = page.GetByTestId("search-submit");
private readonly ILocator _searchCompleted = page.GetByTestId("search_completed");

public ILocator SearchInput { get; } = page.GetByTestId("search-query");

public ILocator SearchTerm { get; } = page.GetByTestId("search-term");

public ILocator ProductNames { get; } = page.GetByTestId("product-name");

public async Task SearchAsync(string term)
{
    await SearchInput.FillAsync(term);
    await _searchButton.ClickAsync();
    await _searchCompleted.WaitForAsync();
}

// ProductSearchTests.cs
[Test]
public async Task Search_lists_only_matching_products()
{
    const string term = "pliers";

    await _homePage.SearchAsync(term);

    await Expect(_homePage.SearchTerm).ToHaveTextAsync(term);
    await Expect(_homePage.ProductNames.First).ToBeVisibleAsync();
    var names = await _homePage.ProductNames.AllInnerTextsAsync();
    Assert.That(names, Has.All.Contains(term).IgnoreCase);
}
```

The full versions are `src/PlaywrightChallenge.Framework/Pages/HomePage.cs` and `tests/PlaywrightChallenge.Tests/Ui/ProductSearchTests.cs`.
