using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

public interface ILoggerWrapper
{
    void LogWarning(string message, params object[] messageArgs);

    void LogMessage(string message, MessageImportance importance = MessageImportance.Low, params object[] messageArgs);
}