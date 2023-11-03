namespace Workleap.OpenApi.MSBuild;

public interface ISpectralManager
{
    public Task InstallSpectralAsync(CancellationToken cancellationToken);

    public Task RunSpectralAsync(IEnumerable<string> swaggerDocumentPaths, string rulesetUrl, CancellationToken cancellationToken);
}