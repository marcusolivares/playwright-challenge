namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>Base class for tests against the local Flaky Shop practice app: <see cref="UiTestBase"/>, pointed at FlakyShopUrl.</summary>
[Category(TestCategories.Lab)]
public abstract class FlakyShopTestBase : UiTestBase
{
    protected override BrowserNewContextOptions CreateContextOptions()
    {
        var options = base.CreateContextOptions();
        options.BaseURL = Settings.FlakyShopUrl;
        return options;
    }
}
