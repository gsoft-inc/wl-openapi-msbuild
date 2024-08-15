#pragma warning disable CA1861
using CliWrap;
using CliWrap.Buffered;
using Meziantou.Framework;

namespace Workleap.OpenApi.MSBuild.Tests;

public sealed class SystemTestFixture : IAsyncLifetime
{
    private readonly TemporaryDirectory _packageDirectory = TemporaryDirectory.Create();

    public string PackageDirectory => this._packageDirectory.FullPath;

    public async Task InitializeAsync()
    {
        var projectPath = PathHelpers.GetGitRoot() / "src" / "Workleap.OpenApi.MSBuild" / "Workleap.OpenApi.MSBuild.csproj";
        _ = await Cli.Wrap("dotnet")
             .WithArguments(["pack", projectPath, "--configuration", "Release", "--output", this._packageDirectory.FullPath])
             .ExecuteBufferedAsync();
    }

    public Task DisposeAsync()
    {
        this._packageDirectory.Dispose();
        return Task.CompletedTask;
    }
}
