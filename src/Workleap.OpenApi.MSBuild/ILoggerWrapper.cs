using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

public interface ILoggerWrapper
{
    void LogWarning(string message, params object[] messageArgs);

    void LogMessage(string message, params object[] messageArgs);
}