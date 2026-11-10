using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class BlazorRestoreTests
{
    [Fact]
    public async Task ProjectOnlyDockerRestoreIncludesBlazorFrameworkAssets()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "InventarioCPR.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var fixture = Path.Combine(Path.GetTempPath(), "opencode", "blazor-restore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        try
        {
            // Reproduce the first Docker layer: only the csproj exists, no .razor source files.
            var project = Path.Combine(fixture, "InventarioCPR.csproj");
            File.Copy(Path.Combine(root.FullName, "InventarioCPR", "InventarioCPR.csproj"), project);
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = fixture, RedirectStandardOutput = true,
                RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };
            start.ArgumentList.Add("restore");
            start.ArgumentList.Add(project);
            start.ArgumentList.Add("--ignore-failed-sources");
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await process.WaitForExitAsync(timeout.Token);
            Assert.True(process.ExitCode == 0, await stdout + await stderr);
            using var assets = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(fixture, "obj", "project.assets.json")));
            Assert.True(assets.RootElement.GetProperty("libraries").EnumerateObject().Any(
                library => library.Name.StartsWith("Microsoft.AspNetCore.App.Internal.Assets/", StringComparison.Ordinal)),
                "Restoring only the csproj must include Microsoft.AspNetCore.App.Internal.Assets; otherwise Docker omits blazor.web.js.");
        }
        finally { Directory.Delete(fixture, recursive: true); }
    }
}
