using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using StudyOrganizer.Api.Configuration;

namespace StudyOrganizer.Api.Tests.Configuration;

public sealed class WorktreeSettingsTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"study-organizer-settings-{Guid.NewGuid():N}");

    [Fact]
    public void HiddenContentRoot_LoadsBaseAndEnvironmentSettings()
    {
        var builder = CreateBuilder(hidden: true);
        using var provider = WorktreeSettings.Load(builder);

        Assert.Equal("fixture-issuer", builder.Configuration["WorktreeSettingsTest:Issuer"]);
        Assert.Equal("environment-audience", builder.Configuration["WorktreeSettingsTest:Audience"]);
    }

    [Fact]
    public void HiddenContentRoot_PreservesHigherPriorityOverrides()
    {
        var builder = CreateBuilder(hidden: true, ["--WorktreeSettingsTest:Issuer=command-issuer"]);
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["WorktreeSettingsTest:Audience"] = "override-audience"
            });
        using var provider = WorktreeSettings.Load(builder);

        Assert.Equal("command-issuer", builder.Configuration["WorktreeSettingsTest:Issuer"]);
        Assert.Equal("override-audience", builder.Configuration["WorktreeSettingsTest:Audience"]);
        Assert.Equal("15", builder.Configuration["WorktreeSettingsTest:ExpiresInMinutes"]);
    }

    [Fact]
    public void OrdinaryContentRoot_KeepsDefaultLoading()
    {
        var builder = CreateBuilder(hidden: false);
        using var provider = WorktreeSettings.Load(builder);

        Assert.Null(provider);
        Assert.Equal("fixture-issuer", builder.Configuration["WorktreeSettingsTest:Issuer"]);
        Assert.Equal("environment-audience", builder.Configuration["WorktreeSettingsTest:Audience"]);
    }

    private WebApplicationBuilder CreateBuilder(bool hidden, string[]? args = null)
    {
        var contentRoot = Path.Combine(
            temporaryDirectory,
            hidden ? ".worktrees" : "worktrees",
            "api");
        Directory.CreateDirectory(contentRoot);
        if (hidden)
        {
            var worktreeDirectory = Path.GetDirectoryName(contentRoot)!;
            File.SetAttributes(worktreeDirectory,
                File.GetAttributes(worktreeDirectory) | FileAttributes.Hidden);
        }
        File.WriteAllText(Path.Combine(contentRoot, "appsettings.json"),
            """
            { "WorktreeSettingsTest": { "Issuer": "fixture-issuer", "Audience": "base-audience", "ExpiresInMinutes": 15 } }
            """);
        File.WriteAllText(Path.Combine(contentRoot, "appsettings.Testing.json"),
            """
            { "WorktreeSettingsTest": { "Audience": "environment-audience" } }
            """);
        if (hidden)
        {
            foreach (var settingsFile in Directory.EnumerateFiles(contentRoot))
            {
                File.SetAttributes(settingsFile,
                    File.GetAttributes(settingsFile) | FileAttributes.Hidden);
            }
        }

        return WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRoot,
            EnvironmentName = "Testing",
            Args = args ?? []
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
