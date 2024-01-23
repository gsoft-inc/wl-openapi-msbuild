namespace Workleap.OpenApi.MSBuild;

internal interface ISpectralManager
{
    public Task InstallSpectralAsync(CancellationToken cancellationToken);

    public Task RunSpectralAsync(IEnumerable<string> swaggerDocumentPaths, string rulesetUrl, CancellationToken cancellationToken);
}