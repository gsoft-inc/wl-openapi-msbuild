using Microsoft.Build.Utilities;

namespace Workleap.OpenApi.MSBuild;

public interface ILoggerWrapper
{
    TaskLoggingHelper Helper { get; set; }
}