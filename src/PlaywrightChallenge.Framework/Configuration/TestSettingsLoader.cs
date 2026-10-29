using Microsoft.Extensions.Configuration;

namespace PlaywrightChallenge.Framework.Configuration;

/// <summary>
/// Builds <see cref="TestSettings"/> from, in increasing priority:
/// <list type="number">
/// <item>appsettings.json (committed defaults, no secrets)</item>
/// <item>appsettings.{E2E_ENVIRONMENT}.json (when E2E_ENVIRONMENT is set, e.g. "Staging")</item>
/// <item>appsettings.local.json (git-ignored, per developer)</item>
/// <item>E2E_* environment variables, with "__" as the section separator (CI secrets), e.g. E2E_Database__ConnectionString</item>
/// </list>
/// </summary>
public static class TestSettingsLoader
{
    public const string EnvironmentVariablePrefix = "E2E_";
    public const string EnvironmentNameVariable = "E2E_ENVIRONMENT";

    public static TestSettings Load(string? basePath = null)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath ?? AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        var environmentName = Environment.GetEnvironmentVariable(EnvironmentNameVariable);
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.AddJsonFile($"appsettings.{environmentName}.json", optional: false);
        }

        var configuration = builder
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables(EnvironmentVariablePrefix)
            .Build();

        var settings = configuration.Get<TestSettings>()
            ?? throw new InvalidOperationException("appsettings.json is empty.");

        settings.Validate();
        return settings;
    }
}
