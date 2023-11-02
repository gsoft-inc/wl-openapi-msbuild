using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralManager : ISpectralManager
{
    private const string SpectralVersion = "6.11.0";

    private readonly IProcessWrapper _processWrapper;
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly string _toolDirectory;

    public SpectralManager(IProcessWrapper processWrapper, ILoggerWrapper loggerWrapper, string openApiToolsDirectoryPath)
    {
        this._processWrapper = processWrapper;
        this._loggerWrapper = loggerWrapper;
        this._toolDirectory = Path.Combine(openApiToolsDirectoryPath, "spectral", SpectralVersion);
    }

    public async Task InstallSpectralAsync(CancellationToken cancellationToken)
    {
        this.CreateRequiredDirectories();
        var executableFileName = this.GetSpectralFileName();

        var url = $"https://github.com/stoplightio/spectral/releases/download/v{SpectralVersion}/{executableFileName}";

        await this.DownloadFileAsync(url, Path.Combine(this._toolDirectory, executableFileName), cancellationToken);
    }

    private void CreateRequiredDirectories()
    {
        Directory.CreateDirectory(this._toolDirectory);
    }

    private string GetSpectralFileName()
    {
        var osType = GetOperatingSystem();
        var architecture = GetArchitecture();

        if (osType == "linux")
        {
            var distro = File.Exists("/etc/os-release") ? File.ReadAllText("/etc/os-release") : string.Empty;
            if (distro.Contains("Alpine Linux"))
            {
                osType = "alpine";
                this._loggerWrapper.Helper.LogMessage(MessageImportance.Low, "Installing on Alpine Linux.");
            }
        }

        var fileName = $"spectral-{osType}-{architecture}";

        if (osType == "win")
        {
            fileName = "spectral.exe";
            this._loggerWrapper.Helper.LogMessage(MessageImportance.Low, "Installing on Windows.");
        }

        return fileName;
    }

    private async Task DownloadFileAsync(string url, string destination, CancellationToken cancellationToken)
    {
        if (File.Exists(destination))
        {
            this._loggerWrapper.Helper.LogMessage(MessageImportance.Low, "File already exist");

            return;
        }

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (!response.IsSuccessStatusCode)
        {
            using var retryResponse = await httpClient.GetAsync(url, cancellationToken);

            if (retryResponse.IsSuccessStatusCode)
            {
                await SaveFileFromResponseAsync(destination, retryResponse, cancellationToken);
            }
            else
            {
                throw new OpenApiTaskFailedException("Spectral could not be installed.");
            }
        }
        else
        {
            await SaveFileFromResponseAsync(destination, response, cancellationToken);
        }
    }

    private static async Task SaveFileFromResponseAsync(string destination, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var fileTarget = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        using var fileStream = await response.Content.ReadAsStreamAsync();

        await fileStream.CopyToAsync(fileTarget, 1024, cancellationToken);
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