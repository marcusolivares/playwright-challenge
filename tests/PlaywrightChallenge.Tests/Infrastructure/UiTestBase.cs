using Microsoft.Playwright.NUnit;
using PlaywrightChallenge.Framework.Configuration;

namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>
/// Base class for browser tests. Every test gets:
/// <list type="bullet">
/// <item>its own <see cref="IBrowserContext"/> and <see cref="IPage"/>, so no cookies or storage leak between tests;</item>
/// <item>trace, screenshot (and optionally video) attached to the NUnit result when it fails.</item>
/// </list>
/// </summary>
public abstract class UiTestBase : BrowserTest
{
    protected static TestSettings Settings => TestEnvironment.Settings;

    protected IBrowserContext Context { get; private set; } = null!;

    protected IPage Page { get; private set; } = null!;

    [SetUp]
    public async Task SetUpBrowserAsync()
    {
        // Lets page objects use GetByTestId() with the app's own attribute (data-test, data-cy, ...).
        Playwright.Selectors.SetTestIdAttribute(Settings.TestIdAttribute);

        Context = await Browser.NewContextAsync(CreateContextOptions());

        if (Settings.Artifacts.TraceOnFailure)
        {
            await Context.Tracing.StartAsync(new()
            {
                Title = TestContext.CurrentContext.Test.FullName,
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task TearDownBrowserAsync()
    {
        var failed = TestArtifacts.CurrentTestFailed;

        await SaveFailureArtifactsAsync(failed);

        // Closing the context also finishes writing the video, if one is being recorded.
        await Context.DisposeAsync();
        await KeepVideoOnlyOnFailureAsync(failed);
    }

    protected virtual BrowserNewContextOptions CreateContextOptions() => new()
    {
        BaseURL = Settings.BaseUrl,
        Locale = Settings.Locale,
        ViewportSize = new() { Width = Settings.Viewport.Width, Height = Settings.Viewport.Height },
        RecordVideoDir = Settings.Artifacts.VideoOnFailure ? TestArtifacts.DirectoryFor("videos") : null,
    };

    private async Task SaveFailureArtifactsAsync(bool failed)
    {
        if (failed && Settings.Artifacts.ScreenshotOnFailure)
        {
            var screenshot = TestArtifacts.FileFor("screenshots", ".png");
            await Page.ScreenshotAsync(new() { Path = screenshot, FullPage = true });
            TestContext.AddTestAttachment(screenshot, "Screenshot at the moment of failure");
        }

        if (Settings.Artifacts.TraceOnFailure)
        {
            var trace = failed ? TestArtifacts.FileFor("traces", ".zip") : null;
            await Context.Tracing.StopAsync(new() { Path = trace });

            if (trace is not null)
            {
                TestContext.AddTestAttachment(trace, "Playwright trace (open with: playwright.ps1 show-trace <file>)");
            }
        }
    }

    private async Task KeepVideoOnlyOnFailureAsync(bool failed)
    {
        if (Page.Video is not { } video)
        {
            return;
        }

        if (failed)
        {
            TestContext.AddTestAttachment(await video.PathAsync(), "Video of the failed test");
        }
        else
        {
            await video.DeleteAsync();
        }
    }
}
