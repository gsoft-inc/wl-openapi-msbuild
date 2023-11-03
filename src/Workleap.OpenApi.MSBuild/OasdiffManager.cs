using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class OasdiffManager : IOasdiffManager
{
    private const string OasdiffVersion = "1.9.2";

    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly string _toolDirectory;

    public OasdiffManager(IProcessWrapper processWrapper, string openApiToolsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._httpClientWrapper = httpClientWrapper;
        this._processWrapper = processWrapper;
        this._toolDirectory = Path.Combine(openApiToolsDirectoryPath, "oasdiff", OasdiffVersion);
    }

    public async Task InstallOasdiffAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(this._toolDirectory);

        var oasdiffFileName = GetOasdiffFileName();
        var url = $"https://github.com/Tufin/oasdiff/releases/download/v{OasdiffVersion}/{oasdiffFileName}";

        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, Path.Combine(this._toolDirectory, oasdiffFileName), cancellationToken);
        await this.DecompressDownloadedFileAsync(oasdiffFileName, cancellationToken);
    }

    private async Task DecompressDownloadedFileAsync(string oasdiffFileName, CancellationToken cancellationToken)
    {
        var pathToCompressedFile = Path.Combine(this._toolDirectory, oasdiffFileName);
        await this._processWrapper.RunProcessAsync("tar", new[] { "-xzf", $"{pathToCompressedFile}", "-C", $"{this._toolDirectory}" }, cancellationToken);
    }

    private static string GetOasdiffFileName()
    {
        var osType = GetOperatingSystem();
        var architecture = GetArchitecture();

        var fileName = $"oasdiff_{OasdiffVersion}_{osType}_{architecture}.tar.gz";

        if (osType == "macos")
        {
            fileName = $"oasdiff_{OasdiffVersion}_darwin_all.tar.gz";
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
            return "windows";
        }

        throw new OpenApiTaskFailedException("Unknown operating system encountered");
    }

    private static string GetArchitecture()
    {
        if (RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            return "amd64";
        }

        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            return "arm64";
        }

        throw new OpenApiTaskFailedException("Unknown processor architecture encountered");
    }
}