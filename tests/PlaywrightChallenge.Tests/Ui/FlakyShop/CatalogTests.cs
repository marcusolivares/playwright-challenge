namespace PlaywrightChallenge.Tests.Ui.FlakyShop;

public sealed class CatalogTests : FlakyShopTestBase
{
    private CatalogPage _catalogPage = null!;

    [SetUp]
    public async Task OpenCatalogAsync()
    {
        _catalogPage = new CatalogPage(Page);
        await _catalogPage.GotoAsync();
    }

    [Test]
    public async Task Adding_a_product_puts_it_in_the_cart()
    {
        await _catalogPage.AddToCartAsync("Hand saw");

        await Expect(_catalogPage.CartItems).ToHaveTextAsync(["Hand saw"]);
    }
}
