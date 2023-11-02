namespace Workleap.OpenApi.MSBuild;

public interface IProcessWrapper
{
    public Task<int> RunProcessAsync(string filename, string[] arguments, CancellationToken cancellationToken);
}