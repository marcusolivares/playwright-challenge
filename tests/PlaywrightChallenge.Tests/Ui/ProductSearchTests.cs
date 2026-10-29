namespace PlaywrightChallenge.Tests.Ui;

[Category(TestCategories.Ui)]
public sealed class ProductSearchTests : UiTestBase
{
    private HomePage _homePage = null!;

    [SetUp]
    public async Task OpenHomePageAsync()
    {
        _homePage = new HomePage(Page);
        await _homePage.GotoAsync();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public async Task Search_lists_only_matching_products()
    {
        const string term = "pliers";

        await _homePage.SearchAsync(term);

        await Expect(_homePage.SearchTerm).ToHaveTextAsync(term);
        await Expect(_homePage.ProductNames.First).ToBeVisibleAsync();
        var names = await _homePage.ProductNames.AllInnerTextsAsync();
        Assert.That(names, Has.All.Contains(term).IgnoreCase);
    }

    [Test]
    public async Task Search_without_matches_says_so()
    {
        await _homePage.SearchAsync(UniqueId.Next());

        await Expect(_homePage.NoResults).ToHaveTextAsync(UiTexts.NoProductsFound);
    }
}
