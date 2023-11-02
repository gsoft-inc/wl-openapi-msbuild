using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralManager : ISpectralManager
{
    private const string SpectralVersion = "6.11.0";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _toolDirectory;

    public SpectralManager(LoggerWrapper loggerWrapper, string openApiToolsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
        this._toolDirectory = Path.Combine(openApiToolsDirectoryPath, "spectral", SpectralVersion);
    }

    public async Task InstallSpectralAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(this._toolDirectory);

        var executableFileName = GetSpectralFileName();
        var url = $"https://github.com/stoplightio/spectral/releases/download/v{SpectralVersion}/{executableFileName}";

        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, Path.Combine(this._toolDirectory, executableFileName), cancellationToken);
    }

    private static string GetSpectralFileName()
    {
        var osType = GetOperatingSystem();
        var architecture = GetArchitecture();

        if (osType == "linux")
        {
            var distro = File.Exists("/etc/os-release") ? File.ReadAllText("/etc/os-release") : string.Empty;
            if (distro.Contains("Alpine Linux"))
            {
                osType = "alpine";
            }
        }

        var fileName = $"spectral-{osType}-{architecture}";

        if (osType == "win")
        {
            fileName = "spectral.exe";
        }

        return fileName;
    }

    private static string GetOperatingSystem()
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
            return "win";
        }

        throw new OpenApiTaskFailedException("Unknown operating system encountered");
    }

    private static string GetArchitecture()
    {
        if (RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            return "x64";
        }

        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            return "arm64";
        }

        throw new OpenApiTaskFailedException("Unknown processor architecture encountered");
    }
}