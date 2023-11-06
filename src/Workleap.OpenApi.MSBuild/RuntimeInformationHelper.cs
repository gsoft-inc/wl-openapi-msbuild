using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal static class RuntimeInformationHelper
{
    internal static string GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macos";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        throw new OpenApiTaskFailedException("Unknown operating system encountered");
    }

    internal static string GetArchitecture(string? prefix = default)
        => RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => string.IsNullOrEmpty(prefix) ? "x64" : $"{prefix}64",
            Architecture.Arm64 => "arm64",
            _ => throw new OpenApiTaskFailedException("Unknown processor architecture encountered"),
        };
}