namespace Workleap.OpenApi.MSBuild;

internal interface IOasdiffManager
{
    Task InstallOasdiffAsync(CancellationToken cancellationToken);
}