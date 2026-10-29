using System.Text.RegularExpressions;

namespace PlaywrightChallenge.Tests.Ui.FlakyShop;

public sealed class CheckoutTests : FlakyShopTestBase
{
    private CheckoutPage _checkoutPage = null!;

    [SetUp]
    public async Task OpenCheckoutAsync()
    {
        _checkoutPage = new CheckoutPage(Page);
        await _checkoutPage.GotoAsync();
    }

    [Test]
    public async Task Placing_an_order_confirms_it()
    {
        await _checkoutPage.PlaceOrderAsync();

        await Expect(_checkoutPage.Confirmation).ToHaveTextAsync(new Regex(@"^Order #\d+ confirmed$"));
    }
}
