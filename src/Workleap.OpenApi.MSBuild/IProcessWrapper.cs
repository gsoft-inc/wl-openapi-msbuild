using CliWrap.Buffered;

namespace Workleap.OpenApi.MSBuild;

public interface IProcessWrapper
{
    public Task<BufferedCommandResult> RunProcessAsync(string filename, string[] arguments, CancellationToken cancellationToken);
}