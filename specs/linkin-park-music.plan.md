# Linkin Park music browsing test plan

Source: codegen recording. App: https://linkinpark.com (a third-party production site, not the public demo site or the Flaky Shop; replayed live on 2026-10-29 at 1920x1080). Page objects: none to reuse (`HomePage` is the Toolshop); new `LinkinParkHomePage`, `MusicPage` and `ReleaseDetailsModal` in `src/PlaywrightChallenge.Framework/Pages/LinkinPark/`, plus `Routes.LinkinPark.Home = "/"` and `Routes.LinkinPark.Music = "/music"`.

Status: ready. `BaseUrl` points at practicesoftwaretesting.com, so this suite has its own host setting, `TestSettings.LinkinParkUrl` (default `https://linkinpark.com`, overridable with `E2E_LinkinParkUrl`), which already exists. The generator also creates:

- `tests/PlaywrightChallenge.Tests/Infrastructure/LinkinParkTestBase.cs`: `LinkinParkTestBase : UiTestBase` with `[Category(TestCategories.External)]`, setting `options.BaseURL = Settings.LinkinParkUrl` in `CreateContextOptions`, like `FlakyShopTestBase`;
- `global using PlaywrightChallenge.Framework.Pages.LinkinPark;` in `tests/PlaywrightChallenge.Tests/GlobalUsings.cs`.

Category: `External` (already in `TestCategories` and excluded from CI), because this is a production site we don't own (see Risks).

Proposed page objects (locators checked live):

- `LinkinParkHomePage`
  - `_startupModal = page.Locator(".modal.banner")` (CSS, last resort: the modal has no role, label or test id)
  - `_closeStartupModalButton = _startupModal.GetByRole(AriaRole.Button)` (its only button; it has no accessible name)
  - `_musicLink = page.GetByRole(AriaRole.Link, new() { Name = "Music", Exact = true })`
  - `ReadyIndicator => _closeStartupModalButton`: the modal is rendered client-side after hydration, so it proves the page is interactive.
  - `GotoAsync() => NavigateAsync(Routes.LinkinPark.Home)`
  - `CloseStartupModalAsync()`: clicks the close button, then waits for `_startupModal` to be `Detached`.
  - `Task<MusicPage> OpenMusicAsync()`: clicks `_musicLink` and returns `MusicPage` after `WaitUntilReadyAsync()`.
- `MusicPage`
  - `ReleaseTitles = page.Locator(".release-card").GetByRole(AriaRole.Heading)`; `ReadyIndicator => ReleaseTitles.First`
  - `ReleaseCard(string title)`: `.release-card` filtered by `Has = GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })`
  - `ReleaseCardDate(string title)`: `ReleaseCard(title).Locator(".release-date")`
  - `Task<ReleaseDetailsModal> OpenReleaseAsync(string title)`: clicks the card's heading and returns the modal after `WaitUntilReadyAsync()`.
- `ReleaseDetailsModal`
  - root `page.Locator(".modal.release")`; `Title` = root `.title`; `ReleaseDate` = root `.release-date`; `Tracks` = root `.track`
  - `Track(string title)`: `Tracks` filtered by `Has = Locator(".track-title").GetByText(title, new() { Exact = true })`
  - `TrackNumber(string title)` = `Track(title).Locator(".track-number")`; `TrackLength(string title)` = `Track(title).Locator(".track-length")`
  - `ReadyIndicator => Title`

## 1. Browsing the discography

### 1.1 Opening_an_album_from_the_music_page_shows_its_tracklist
- Class: `MusicTests` in `tests/PlaywrightChallenge.Tests/Ui/LinkinPark/`, deriving from the proposed `LinkinParkTestBase`
- Category: External, inherited from `LinkinParkTestBase` (no `[Category(TestCategories.Ui)]` on the class, so CI skips it)
- Starts at: `/` on `LinkinParkUrl`
- Data: none (read-only CMS catalogue)
- Recorded lines: 1 (`GetByRole(AriaRole.Button).Nth(2).ClickAsync()`, the startup modal's close button), 2 (Music link), 4 (`Expect` heading "The Hunting Party" visible), 5 (click the heading), 6 (`Expect` "Wastelands" visible). Dropped: 3 (right-click on the heading) and 7 (click on "Wastelands"), neither of which has an effect.
- Steps:
  1. Open the home page and close the startup promo modal.
     - expect: none in the test. This is a precondition: the modal covers the main navigation, and the action waits until the modal is removed from the DOM. Observed: "Unshatter" movie promo, rendered about 300 ms after the `load` event.
  2. Open Music from the main navigation.
     - expect: the release list has a card titled exactly "The Hunting Party" (recorded, strengthened from heading visibility to the card in the list) whose release date is "17 Jun 2014" (inferred). Observed: 15 cards under the default "Albums" filter, starting with "FROM ZERO (DELUXE EDITION)".
     - expect: the URL path is `/music` (inferred, alongside the content check only).
  3. Open "The Hunting Party" by clicking its title.
     - expect: the release details modal's title is "The Hunting Party" (inferred).
     - expect: the modal's release date is "17 Jun 2014" (inferred).
     - expect: the tracklist has a "Wastelands" row (recorded as `GetByText("Wastelands")` visible), numbered "6" with length "3:15" (inferred).
     - expect: the tracklist has 12 rows (inferred). Observed: the modal also says "Tracks: 12 |" and "Time: 45 MIN", label "Warner Bros.", link "Buy / Listen" to http://lprk.co/TheHuntingParty.
- Risks:
  - Startup modal: it is rendered client-side after hydration (absent from the server HTML) about 300 ms after `load`, so `GotoAsync` returns before it exists. It covers the "Music" link (the hit test at the link's centre returns the modal), so a click on "Music" can be intercepted. Wait for the close button as the ready indicator, close the modal, and wait for it to detach. It is shown once per browser (`localStorage.hasSeenStartupModal_home`), so every fresh test context sees it. It is a marketing campaign: if the site stops showing it, the ready wait fails and the page object needs a different ready indicator.
  - Music page: client-side navigation; the URL changes first, and about 0.9 s later the 15 album cards and the active "Albums" filter render together, after `GET /api/content/releases` (64 releases with full tracklists). There is no unfiltered intermediate state, so waiting for the first card heading is enough.
  - Release details: opening an album makes no request (the data is already loaded). Wait for the modal title.
  - "Wastelands" is also a single under "Eps & Singles", so a page-wide `GetByText("Wastelands")` can match a release card under other filters. Scope the check to the modal's tracklist.
  - Changing data: CMS content on a site we don't own. The 2014 album's tracklist is stable, but titles, dates or card markup can change without notice. Bot protection (reCAPTCHA is loaded) or a region-dependent consent banner (Termly; none appeared in this session) could block headless runs from CI.
  - No roles or test ids exist on cards, modals or track rows, so their locators are CSS class selectors scoped to the modal or card. Re-check them when the site is redesigned.

## Recording

```csharp
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task MyTest()
    {
        await Page.GetByRole(AriaRole.Button).Nth(2).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Music" }).ClickAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "The Hunting Party" }).ClickAsync(new LocatorClickOptions
        {
            Button = MouseButton.Right,
        });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "The Hunting Party" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "The Hunting Party" }).ClickAsync();
        await Expect(Page.GetByText("Wastelands")).ToBeVisibleAsync();
        await Page.GetByText("Wastelands").ClickAsync();
    }
}
```
