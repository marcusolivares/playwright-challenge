namespace PlaywrightChallenge.Tests.Ui.FlakyShop;

public sealed class OrdersTests : FlakyShopTestBase
{
    private OrdersPage _ordersPage = null!;

    [SetUp]
    public async Task OpenOrdersPageAsync()
    {
        _ordersPage = new OrdersPage(Page);
        await _ordersPage.GotoAsync();
    }

    [Test]
    public async Task Loading_orders_lists_all_five()
    {
        await _ordersPage.LoadOrdersAsync();

        // The orders API is slow; give it a second.
        await Page.WaitForTimeoutAsync(1000);

        Assert.That(await _ordersPage.Rows.CountAsync(), Is.EqualTo(5));
    }
}
