using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.FlakyShop;

/// <summary>Product grid with an add-to-cart button per product, and the cart below it.</summary>
public sealed class CatalogPage(IPage page) : BasePage(page)
{
    // The grid lists products in catalog order.
    private static readonly string[] CatalogOrder =
    [
        "Claw hammer",
        "Hand saw",
        "Circular saw",
        "Combination pliers",
        "Screwdriver set",
        "Tape measure",
        "Spirit level",
        "Utility knife",
    ];

    public ILocator ProductCards { get; } = page.GetByTestId("product");

    public ILocator CartItems { get; } = page.GetByTestId("cart-item");

    protected override ILocator ReadyIndicator => ProductCards.First;

    public Task GotoAsync() => NavigateAsync(Routes.FlakyShop.Catalog);

    public Task AddToCartAsync(string productName)
    {
        var position = Array.IndexOf(CatalogOrder, productName) + 1;
        return Page.Locator($".cards > li:nth-child({position}) button").ClickAsync();
    }
}
