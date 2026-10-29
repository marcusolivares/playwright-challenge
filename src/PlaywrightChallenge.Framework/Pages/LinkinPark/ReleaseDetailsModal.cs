using Microsoft.Playwright;

namespace PlaywrightChallenge.Framework.Pages.LinkinPark;

/// <summary>Release details modal on the Music page: title, date and tracklist.</summary>
public sealed class ReleaseDetailsModal(IPage page) : BasePage(page)
{
    // No roles or test ids exist in the modal, so its parts are found by CSS class, scoped to the modal.
    private readonly ILocator _root = page.Locator(".modal.release");

    public ILocator Title => _root.Locator(".title");

    public ILocator ReleaseDate => _root.Locator(".release-date");

    public ILocator Tracks => _root.Locator(".track");

    protected override ILocator ReadyIndicator => Title;

    public ILocator Track(string title) =>
        Tracks.Filter(new() { Has = Page.Locator(".track-title").GetByText(title, new() { Exact = true }) });

    public ILocator TrackNumber(string title) => Track(title).Locator(".track-number");

    public ILocator TrackLength(string title) => Track(title).Locator(".track-length");
}
