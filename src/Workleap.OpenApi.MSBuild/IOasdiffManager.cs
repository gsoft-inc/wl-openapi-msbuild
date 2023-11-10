namespace Workleap.OpenApi.MSBuild;

internal interface IOasdiffManager
{
    Task InstallOasdiffAsync(CancellationToken cancellationToken);
    
    Task RunOasdiffAsync(IEnumerable<string> openApiSpecFiles, IEnumerable<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken);
}