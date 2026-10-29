using Microsoft.Playwright;

namespace PlaywrightChallenge.Framework.Pages.LinkinPark;

/// <summary>Music page: the release list, "Albums" filter by default.</summary>
public sealed class MusicPage(IPage page) : BasePage(page)
{
    // Cards have no role or test id, so they are found by CSS class.
    private readonly ILocator _releaseCards = page.Locator(".release-card");

    public ILocator ReleaseTitles { get; } = page.Locator(".release-card").GetByRole(AriaRole.Heading);

    // Cards and the default filter render together after the releases request, so the first title proves the list is there.
    protected override ILocator ReadyIndicator => ReleaseTitles.First;

    public ILocator ReleaseCard(string title) =>
        _releaseCards.Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true }) });

    public ILocator ReleaseCardDate(string title) => ReleaseCard(title).Locator(".release-date");

    /// <summary>Opening an album makes no request; the modal is ready once its title shows.</summary>
    public async Task<ReleaseDetailsModal> OpenReleaseAsync(string title)
    {
        await ReleaseCard(title).GetByRole(AriaRole.Heading, new() { Name = title, Exact = true }).ClickAsync();
        var modal = new ReleaseDetailsModal(Page);
        await modal.WaitUntilReadyAsync();
        return modal;
    }
}
