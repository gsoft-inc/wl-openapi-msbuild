using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

internal sealed class LoggerWrapper : ILoggerWrapper
{
    public LoggerWrapper(TaskLoggingHelper helper)
    {
        this.Helper = helper;
    }

    public TaskLoggingHelper Helper { get; set; }

    public void LogWarning(string message, params object[] messageArgs)
    {
        this.Helper.LogWarning(message, messageArgs);
    }

    public void LogMessage(string message, params object[] messageArgs)
    {
        this.Helper.LogMessage(message, messageArgs);
    }
}