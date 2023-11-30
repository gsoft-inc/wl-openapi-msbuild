namespace Workleap.OpenApi.MSBuild;

public interface ISpectralManager
{
    public Task InstallSpectralAsync();

    public Task RunSpectralAsync(IEnumerable<string> swaggerDocumentPaths, string rulesetUrl, CancellationToken cancellationToken);
}