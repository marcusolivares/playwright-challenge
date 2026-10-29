using PlaywrightChallenge.Framework.Configuration;

namespace PlaywrightChallenge.Tests;

/// <summary>
/// Runs once before the whole test run (it applies to every test under the PlaywrightChallenge.Tests namespace).
/// Invalid configuration fails here, before any browser starts.
/// </summary>
[SetUpFixture]
public sealed class GlobalSetup
{
    [OneTimeSetUp]
    public void LoadEnvironment() => TestEnvironment.Initialize(TestSettingsLoader.Load());
}
