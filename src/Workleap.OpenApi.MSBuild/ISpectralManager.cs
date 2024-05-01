namespace Workleap.OpenApi.MSBuild;

internal interface ISpectralManager
{
    public Task InstallSpectralAsync(CancellationToken cancellationToken);

    public Task RunSpectralAsync(IReadOnlyCollection<string> generatedOpenApiDocumentPaths, IReadOnlyCollection<string> sourcedControlOpenApiDocumentPaths, CancellationToken cancellationToken);
}