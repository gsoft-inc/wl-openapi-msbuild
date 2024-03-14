using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

internal sealed class LoggerWrapper : ILoggerWrapper
{
    private readonly TaskLoggingHelper _helper;
    private readonly bool _treatWarningAsError;

    public LoggerWrapper(TaskLoggingHelper helper, bool treatWarningAsError)
    {
        this._helper = helper;
        this._treatWarningAsError = treatWarningAsError;
    }
    
    public void LogMessage(string message, MessageImportance importance = MessageImportance.Low, params object[] messageArgs)
        => this._helper.LogMessage(importance, message, messageArgs);

    public void LogWarning(string message, params object[] messageArgs)
    {
        if (this._treatWarningAsError)
        {
            this._helper.LogError(message, messageArgs);
        }
        else
        {
            this._helper.LogWarning(message, messageArgs);
        }
    }

    public void LogError(string message, params object[] messageArgs)
        => this._helper.LogError(message, messageArgs);
}