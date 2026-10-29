# Templates and API idioms

## Page object

```csharp
using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages;   // PlaywrightChallenge.Framework.Pages.FlakyShop for Flaky Shop pages

/// <summary>One line on what the screen is for.</summary>
public sealed class ProductPage(IPage page) : BasePage(page)
{
    // Used only by actions.
    private readonly ILocator _addToCartButton = page.GetByRole(AriaRole.Button, new() { Name = "Add to cart" });
    private readonly ILocator _addedToast = page.GetByRole(AriaRole.Alert);

    // Asserted on by tests.
    public ILocator Name { get; } = page.GetByTestId("product-name");

    public ILocator Price { get; } = page.GetByTestId("unit-price");

    public ILocator CartQuantity { get; } = page.GetByTestId("cart-quantity");

    // Proves the page is usable for the next action, not merely painted.
    protected override ILocator ReadyIndicator => _addToCartButton;

    public Task GotoAsync(string productId) => NavigateAsync($"{Routes.Product}/{productId}");

    /// <summary>Adds the product and waits until the app confirms it.</summary>
    public async Task AddToCartAsync()
    {
        await _addToCartButton.ClickAsync();
        await _addedToast.WaitForAsync();
    }
}
```

The names above are illustrative; take real ones from the app with playwright-cli.

## Test

```csharp
namespace PlaywrightChallenge.Tests.Ui;     // PlaywrightChallenge.Tests.Ui.FlakyShop for Flaky Shop tests

[Category(TestCategories.Ui)]               // FlakyShopTestBase already adds the Lab category
public sealed class ProductTests : UiTestBase   // FlakyShopTestBase for the Flaky Shop
{
    private HomePage _homePage = null!;

    [SetUp]
    public async Task OpenHomePageAsync()
    {
        _homePage = new HomePage(Page);
        await _homePage.GotoAsync();
    }

    // Plan: specs/search.plan.md 1.2
    [Test]
    public async Task Search_without_matches_says_so()
    {
        var term = UniqueId.Next();                 // per-test data lives in a local, never in a field or const

        await _homePage.SearchAsync(term);

        await Expect(_homePage.NoResults).ToHaveTextAsync(UiTexts.NoProductsFound);
    }
}
```

## Idioms

```csharp
// Locators, in priority order
page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true })
page.GetByLabel("Email address")
page.GetByTestId("search-query")                         // matches data-test="search-query"
items.Filter(new() { HasText = title })                  // an item by its text
cards.Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }) })
// In methods use Page; the constructor parameter page belongs in initializers only (warning CS9107 otherwise).

// Waiting inside page object actions
await _searchCompleted.WaitForAsync();                   // an element the app shows when it is done
await Page.RunAndWaitForResponseAsync(() => _saveButton.ClickAsync(), "**/api/items/save");
await Page.RunAndWaitForResponseAsync(                   // one path, several requests: match method and URL
    () => _addButton.ClickAsync(),
    response => response.Request.Method == "POST" && response.Url.EndsWith("/api/items"));
await _spinner.WaitForAsync(new() { State = WaitForSelectorState.Hidden });   // page objects wait; only tests assert

// Web-first assertions (tests only)
await Expect(_homePage.SearchTerm).ToHaveTextAsync(term);
await Expect(_homePage.ProductNames).ToHaveTextAsync(["Combination Pliers", "Pliers"]);   // the whole list, in order
await Expect(_homePage.ProductNames).ToHaveCountAsync(9);
await Expect(_homePage.ProductNames.First).ToContainTextAsync("Pliers");
await Expect(Page).ToHaveURLAsync(new Regex("/product/"));          // only alongside a content check

// Reading values once, after a web-first assertion proved the final state
await Expect(_homePage.ProductNames).ToHaveCountAsync(9);
var names = await _homePage.ProductNames.AllInnerTextsAsync();
Assert.That(names, Is.Ordered.Ascending);
```
