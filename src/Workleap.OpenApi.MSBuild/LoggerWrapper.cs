using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

internal sealed class LoggerWrapper : ILoggerWrapper
{
    public LoggerWrapper(TaskLoggingHelper helper)
    {
        this.Helper = helper;
    }

    public TaskLoggingHelper Helper { get; set; }
}