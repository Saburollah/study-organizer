using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace StudyOrganizer.Api.Configuration;

public static class WorktreeSettings
{
    public static PhysicalFileProvider? Load(WebApplicationBuilder builder)
    {
        var contentRoot = builder.Environment.ContentRootPath;
        var settingsNames = new[]
        {
            "appsettings.json",
            $"appsettings.{builder.Environment.EnvironmentName}.json"
        };

        var hiddenSettingsSources = builder.Configuration.Sources
            .OfType<JsonConfigurationSource>()
            .Where(source => settingsNames.Contains(source.Path)
                && source.FileProvider is PhysicalFileProvider physical
                && Path.GetFullPath(physical.Root).TrimEnd(Path.DirectorySeparatorChar)
                    == Path.GetFullPath(contentRoot).TrimEnd(Path.DirectorySeparatorChar)
                && File.Exists(Path.Combine(contentRoot, source.Path!))
                && !physical.GetFileInfo(source.Path!).Exists)
            .ToArray();

        if (hiddenSettingsSources.Length == 0)
        {
            return null;
        }

        // Only the known application settings files need access to hidden
        // worktree files. Keep source ordering, including secrets and overrides.
        var provider = new PhysicalFileProvider(contentRoot, ExclusionFilters.None);
        foreach (var source in hiddenSettingsSources)
        {
            var index = builder.Configuration.Sources.IndexOf(source);
            source.FileProvider = provider;
            builder.Configuration.Sources[index] = source;
        }

        return provider;
    }
}
