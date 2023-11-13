using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

internal sealed class LoggerWrapper : ILoggerWrapper
{
    private readonly TaskLoggingHelper _taskLoggingHelper;

    public LoggerWrapper(TaskLoggingHelper helper) => this._taskLoggingHelper = helper;

    public void LogWarning(string message, params object[] messageArgs)
        => this._taskLoggingHelper.LogWarning(message, messageArgs);

    public void LogMessage(string message, params object[] messageArgs)
        => this._taskLoggingHelper.LogMessage(message, messageArgs);
}