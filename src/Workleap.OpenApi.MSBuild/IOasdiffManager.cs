namespace Workleap.OpenApi.MSBuild;

internal interface IOasdiffManager
{
    Task InstallOasdiffAsync(CancellationToken cancellationToken);
    
    Task RunOasdiffAsync(IReadOnlyCollection<string> openApiSpecFiles, IReadOnlyCollection<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken);
}