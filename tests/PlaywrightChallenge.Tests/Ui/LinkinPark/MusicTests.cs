using System.Text.RegularExpressions;

namespace PlaywrightChallenge.Tests.Ui.LinkinPark;

public sealed class MusicTests : LinkinParkTestBase
{
    private LinkinParkHomePage _homePage = null!;

    [SetUp]
    public async Task OpenHomePageAsync()
    {
        _homePage = new LinkinParkHomePage(Page);
        await _homePage.GotoAsync();
        await _homePage.CloseStartupModalAsync();
    }

    // Plan: specs/linkin-park-music.plan.md 1.1
    [Test]
    public async Task Opening_an_album_from_the_music_page_shows_its_tracklist()
    {
        var musicPage = await _homePage.OpenMusicAsync();

        await Expect(musicPage.ReleaseCard("The Hunting Party")).ToHaveCountAsync(1);
        await Expect(musicPage.ReleaseCardDate("The Hunting Party")).ToHaveTextAsync("17 Jun 2014");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/music$"));

        var modal = await musicPage.OpenReleaseAsync("The Hunting Party");

        await Expect(modal.Title).ToHaveTextAsync("The Hunting Party");
        await Expect(modal.ReleaseDate).ToHaveTextAsync("17 Jun 2014");
        await Expect(modal.Tracks).ToHaveCountAsync(12);
        await Expect(modal.TrackNumber("Wastelands")).ToHaveTextAsync("6");
        await Expect(modal.TrackLength("Wastelands")).ToHaveTextAsync("3:15");
    }
}
