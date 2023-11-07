namespace Workleap.OpenApi.MSBuild;

internal interface IOasdiffManager
{
    Task InstallOasdiffAsync(CancellationToken cancellationToken);
    
    Task RunOasdiffAsync(IEnumerable<string> openApiSpecificationFiles, CancellationToken cancellationToken);
}