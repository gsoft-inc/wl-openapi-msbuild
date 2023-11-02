namespace Workleap.OpenApi.MSBuild;

public interface ISpectralManager
{
    public Task InstallSpectralAsync(CancellationToken cancellationToken);
}