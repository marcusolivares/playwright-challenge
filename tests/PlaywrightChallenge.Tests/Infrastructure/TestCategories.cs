namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>Category names for <c>[Category]</c>; filter with <c>dotnet test --filter "TestCategory=Smoke"</c>.</summary>
public static class TestCategories
{
    public const string Ui = "UI";
    public const string Smoke = "Smoke";

    /// <summary>Deliberately flaky practice tests against the local Flaky Shop app; excluded from CI.</summary>
    public const string Lab = "Lab";

    /// <summary>Tests against third-party production sites we don't own; excluded from CI.</summary>
    public const string External = "External";
}
