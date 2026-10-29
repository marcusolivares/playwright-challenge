using Microsoft.Playwright;

namespace PlaywrightChallenge.Framework.Pages;

/// <summary>
/// Base for every page object. A page object owns its locators and exposes user-level actions; it never asserts.
/// Tests decide what "correct" means, using the locators the page exposes and Playwright's web-first assertions.
/// </summary>
public abstract class BasePage(IPage page)
{
    protected IPage Page { get; } = page;

    /// <summary>An element whose presence proves the page has rendered.</summary>
    protected abstract ILocator ReadyIndicator { get; }

    public Task WaitUntilReadyAsync() => ReadyIndicator.WaitForAsync();

    /// <summary>Opens <paramref name="path"/> (relative to BaseUrl) and waits until the page is ready.</summary>
    protected async Task NavigateAsync(string path)
    {
        await Page.GotoAsync(path);
        await WaitUntilReadyAsync();
    }
}
