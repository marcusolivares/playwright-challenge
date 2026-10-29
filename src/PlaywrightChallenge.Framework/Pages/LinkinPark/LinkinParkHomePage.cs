using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.LinkinPark;

/// <summary>Home page of the Linkin Park site: the main navigation and the startup promo modal.</summary>
public sealed class LinkinParkHomePage(IPage page) : BasePage(page)
{
    // The modal has no role, label or test id, so it is found by its CSS classes.
    private readonly ILocator _startupModal = page.Locator(".modal.banner");

    private readonly ILocator _closeStartupModalButton = page.Locator(".modal.banner").GetByRole(AriaRole.Button);

    private readonly ILocator _musicLink = page.GetByRole(AriaRole.Link, new() { Name = "Music", Exact = true });

    // The modal is rendered client-side after hydration (about 300 ms after load), so its close button proves the page is interactive.
    protected override ILocator ReadyIndicator => _closeStartupModalButton;

    public Task GotoAsync() => NavigateAsync(Routes.LinkinPark.Home);

    /// <summary>The modal covers the main navigation; returns once it is gone from the DOM.</summary>
    public async Task CloseStartupModalAsync()
    {
        await _closeStartupModalButton.ClickAsync();
        await _startupModal.WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }

    public async Task<MusicPage> OpenMusicAsync()
    {
        await _musicLink.ClickAsync();
        var musicPage = new MusicPage(Page);
        await musicPage.WaitUntilReadyAsync();
        return musicPage;
    }
}
