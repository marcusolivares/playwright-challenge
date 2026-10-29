using NUnit.Framework.Interfaces;

namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>Where failure artifacts (traces, screenshots, videos) of the current test are written.</summary>
internal static class TestArtifacts
{
    public static bool CurrentTestFailed =>
        TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;

    /// <summary>playwright-artifacts/{kind} under NUnit's work directory (the test output folder by default).</summary>
    public static string DirectoryFor(string kind)
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-artifacts", kind);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string FileFor(string kind, string extension)
    {
        var name = string.Concat(TestContext.CurrentContext.Test.FullName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(DirectoryFor(kind), name + extension);
    }
}
