namespace Workleap.OpenApi.MSBuild;

internal interface ISpectralManager
{
    public Task InstallSpectralAsync(CancellationToken cancellationToken);

    public Task RunSpectralAsync(IReadOnlyCollection<string> openApiDocumentPaths, CancellationToken cancellationToken);
}