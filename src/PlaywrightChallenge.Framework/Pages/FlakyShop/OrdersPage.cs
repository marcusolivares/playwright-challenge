using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.FlakyShop;

/// <summary>Order history, loaded on demand.</summary>
public sealed class OrdersPage(IPage page) : BasePage(page)
{
    private readonly ILocator _loadButton = page.GetByRole(AriaRole.Button, new() { Name = "Load orders" });

    public ILocator Status { get; } = page.GetByTestId("orders-status");

    public ILocator Rows { get; } = page.GetByTestId("order-row");

    protected override ILocator ReadyIndicator => _loadButton;

    public Task GotoAsync() => NavigateAsync(Routes.FlakyShop.Orders);

    public Task LoadOrdersAsync() => _loadButton.ClickAsync();
}
