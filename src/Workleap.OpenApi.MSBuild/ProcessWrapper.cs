using CliWrap;
using CliWrap.Buffered;

namespace Workleap.OpenApi.MSBuild;

internal sealed class ProcessWrapper : IProcessWrapper
{
    private readonly string _workingDirectory;
    private static readonly Dictionary<string, string?> _defaultEnvVars = new();

    public ProcessWrapper(string workingDirectory)
    {
        this._workingDirectory = workingDirectory;
    }

    public async Task<BufferedCommandResult> RunProcessAsync(string filename, string[] arguments, CancellationToken cancellationToken, Dictionary<string, string?>? envVars = null)
    {
        var result = await Cli.Wrap(filename)
            .WithWorkingDirectory(this._workingDirectory)
            .WithValidation(CommandResultValidation.None)
            .WithArguments(arguments)
            .WithEnvironmentVariables(envVars ?? this._defaultEnvVars)
            .ExecuteBufferedAsync(cancellationToken);

        return result;
    }
}