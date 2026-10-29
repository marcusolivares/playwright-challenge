namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>Base class for tests against the Linkin Park official site, a third-party production site: <see cref="UiTestBase"/>, pointed at LinkinParkUrl.</summary>
[Category(TestCategories.External)]
public abstract class LinkinParkTestBase : UiTestBase
{
    protected override BrowserNewContextOptions CreateContextOptions()
    {
        var options = base.CreateContextOptions();
        options.BaseURL = Settings.LinkinParkUrl;
        return options;
    }
}
