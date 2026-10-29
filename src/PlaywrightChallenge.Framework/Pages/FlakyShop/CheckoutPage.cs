using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.FlakyShop;

/// <summary>Payment method choice and the place-order button.</summary>
public sealed class CheckoutPage(IPage page) : BasePage(page)
{
    private readonly ILocator _heading = page.GetByRole(AriaRole.Heading, new() { Name = "Checkout" });
    private readonly ILocator _placeOrderButton = page.GetByRole(AriaRole.Button, new() { Name = "Place order" });

    public ILocator Confirmation { get; } = page.GetByTestId("confirmation");

    protected override ILocator ReadyIndicator => _heading;

    public ILocator PaymentMethod(string name) => Page.GetByRole(AriaRole.Radio, new() { Name = name });

    public Task GotoAsync() => NavigateAsync(Routes.FlakyShop.Checkout);

    public Task PlaceOrderAsync() => _placeOrderButton.ClickAsync();
}
