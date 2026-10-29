namespace PlaywrightChallenge.Tests.Ui.FlakyShop;

public sealed class TasksTests : FlakyShopTestBase
{
    private const string Title = "Write report";

    private TasksPage _tasksPage = null!;

    [SetUp]
    public async Task OpenTasksAsync()
    {
        _tasksPage = new TasksPage(Page);
        await _tasksPage.GotoAsync();
    }

    [Test]
    public async Task New_task_appears_in_the_list()
    {
        await _tasksPage.AddAsync(Title);

        await Expect(_tasksPage.Tasks).ToHaveCountAsync(1);
        await Expect(_tasksPage.Tasks).ToContainTextAsync(Title);
    }

    [Test]
    public async Task Completing_a_task_marks_it_done()
    {
        await _tasksPage.AddAsync(Title);

        await _tasksPage.CompleteAsync(Title);

        await Expect(_tasksPage.TaskNamed(Title)).ToContainTextAsync("Done");
    }
}
