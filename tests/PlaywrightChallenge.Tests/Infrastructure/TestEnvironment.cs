using PlaywrightChallenge.Framework.Configuration;

namespace PlaywrightChallenge.Tests.Infrastructure;

/// <summary>Run-wide singletons, created once by <see cref="GlobalSetup"/>.</summary>
public static class TestEnvironment
{
    private static TestSettings? _settings;

    public static TestSettings Settings =>
        _settings ?? throw new InvalidOperationException("Settings are loaded by GlobalSetup; keep tests under the PlaywrightChallenge.Tests namespace.");

    internal static void Initialize(TestSettings settings) => _settings = settings;
}
