namespace Workleap.OpenApi.MSBuild;

public interface ISpectralManager
{
    public Task<string> InstallSpectralAsync(CancellationToken cancellationToken);

    public Task RunSpectralAsync(IEnumerable<string> swaggerDocumentPaths, string rulesetUrl, string executableFilename, CancellationToken cancellationToken);
}