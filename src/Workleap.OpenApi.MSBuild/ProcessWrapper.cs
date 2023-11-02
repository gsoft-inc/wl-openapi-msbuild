using CliWrap;
using CliWrap.Buffered;

namespace Workleap.OpenApi.MSBuild;

internal sealed class ProcessWrapper : IProcessWrapper
{
    private readonly string _workingDirectory;

    public ProcessWrapper(string workingDirectory)
    {
        this._workingDirectory = workingDirectory;
    }

    public async Task<int> RunProcessAsync(string filename, string[] arguments, CancellationToken cancellationToken)
    {
        var result = await Cli.Wrap(filename)
            .WithWorkingDirectory(this._workingDirectory)
            .WithValidation(CommandResultValidation.None)
            .WithArguments(arguments)
            .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
            .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
            .ExecuteBufferedAsync(cancellationToken);

        return result.ExitCode;
    }
}