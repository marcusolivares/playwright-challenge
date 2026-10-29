namespace PlaywrightChallenge.Tests.Ui.FlakyShop;

public sealed class SearchTests : FlakyShopTestBase
{
    private SearchPage _searchPage = null!;

    [SetUp]
    public async Task OpenSearchAsync()
    {
        _searchPage = new SearchPage(Page);
        await _searchPage.GotoAsync();
    }

    [Test]
    public async Task Search_shows_only_matching_products()
    {
        await _searchPage.SearchAsync("saw");

        await Expect(_searchPage.Results.First).ToBeVisibleAsync();
        var names = await _searchPage.Results.AllInnerTextsAsync();
        Assert.That(names, Has.All.Contains("saw").IgnoreCase);
    }
}
