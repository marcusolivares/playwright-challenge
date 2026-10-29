using Microsoft.Playwright;
using PlaywrightChallenge.Framework.Constants;

namespace PlaywrightChallenge.Framework.Pages.FlakyShop;

/// <summary>The team's shared task list.</summary>
public sealed class TasksPage(IPage page) : BasePage(page)
{
    private readonly ILocator _newTaskInput = page.GetByLabel("New task");
    private readonly ILocator _addButton = page.GetByRole(AriaRole.Button, new() { Name = "Add task" });

    public ILocator Tasks { get; } = page.GetByTestId("task");

    protected override ILocator ReadyIndicator => _newTaskInput;

    public ILocator TaskNamed(string title) => Tasks.Filter(new() { HasText = title });

    public Task GotoAsync() => NavigateAsync(Routes.FlakyShop.Tasks);

    /// <summary>Adds a task and waits until the server has stored it.</summary>
    public async Task AddAsync(string title)
    {
        await _newTaskInput.FillAsync(title);
        await Page.RunAndWaitForResponseAsync(() => _addButton.ClickAsync(), "**/api/tasks");
    }

    public Task CompleteAsync(string title) =>
        TaskNamed(title).GetByRole(AriaRole.Button, new() { Name = "Complete" }).ClickAsync();
}
