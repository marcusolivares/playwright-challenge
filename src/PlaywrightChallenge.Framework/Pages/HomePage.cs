using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages;

/// <summary>Product overview with search.</summary>
public sealed class HomePage(IPage page) : BasePage(page)
{
    private readonly ILocator _searchButton = page.GetByTestId("search-submit");

    // The results container exposes its state in data-test; "search_completed" means the results are rendered.
    private readonly ILocator _searchCompleted = page.GetByTestId("search_completed");

    public ILocator SearchInput { get; } = page.GetByTestId("search-query");

    /// <summary>The term echoed in the "Searched for:" caption.</summary>
    public ILocator SearchTerm { get; } = page.GetByTestId("search-term");

    public ILocator ProductNames { get; } = page.GetByTestId("product-name");

    public ILocator NoResults { get; } = page.GetByTestId("no-results");

    // The initial product list, not the search box: the app renders whichever product response arrives last,
    // so searching before the initial list has loaded can leave the unfiltered products on screen.
    protected override ILocator ReadyIndicator => ProductNames.First;

    public Task GotoAsync() => NavigateAsync(Routes.Home);

    /// <summary>Searches and waits until the results are rendered.</summary>
    public async Task SearchAsync(string term)
    {
        await SearchInput.FillAsync(term);
        await _searchButton.ClickAsync();
        await _searchCompleted.WaitForAsync();
    }
}
