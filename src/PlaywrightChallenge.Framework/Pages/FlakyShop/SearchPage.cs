using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.FlakyShop;

/// <summary>Product search; lists every product until a search is made.</summary>
public sealed class SearchPage(IPage page) : BasePage(page)
{
    private readonly ILocator _searchBox = page.GetByRole(AriaRole.Searchbox, new() { Name = "Search products" });
    private readonly ILocator _searchButton = page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true });

    public ILocator Caption { get; } = page.GetByTestId("results-caption");

    public ILocator Results { get; } = page.GetByTestId("result");

    protected override ILocator ReadyIndicator => _searchBox;

    public Task GotoAsync() => NavigateAsync(Routes.FlakyShop.Search);

    public async Task SearchAsync(string term)
    {
        await _searchBox.FillAsync(term);
        await _searchButton.ClickAsync();
    }
}
