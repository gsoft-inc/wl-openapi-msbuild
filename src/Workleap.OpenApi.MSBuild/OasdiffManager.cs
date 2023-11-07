using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class OasdiffManager : IOasdiffManager
{
    private const string OasdiffVersion = "1.9.2";

    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly string _openApiToolsDirectory;
    private readonly string _oasdiffDirectory;

    public OasdiffManager(IProcessWrapper processWrapper, string openApiToolsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._httpClientWrapper = httpClientWrapper;
        this._processWrapper = processWrapper;
        this._openApiToolsDirectory = openApiToolsDirectoryPath;
        this._oasdiffDirectory = Path.Combine(openApiToolsDirectoryPath, "oasdiff", OasdiffVersion);
    }
    
    public async Task InstallOasdiffAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(this._oasdiffDirectory);

        var oasdiffFileName = GetOasdiffFileName();
        var url = $"https://github.com/Tufin/oasdiff/releases/download/v{OasdiffVersion}/{oasdiffFileName}";

        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, Path.Combine(this._oasdiffDirectory, oasdiffFileName), cancellationToken);
        await this.DecompressDownloadedFileAsync(oasdiffFileName, cancellationToken);
    }

    public async Task RunOasdiffAsync(IEnumerable<string> openApiSpecificationFiles, CancellationToken cancellationToken)
    {
        var oasdiffExecutePath = Path.Combine(this._oasdiffDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "oasdiff.exe" : "oasdiff");
        var reportsPath = Path.Combine(this._openApiToolsDirectory, "reports");
        var baseSpecRelativePath = Path.GetRelativePath(this._openApiToolsDirectory, @"C:\dev\wl-openapi-msbuild\src\WebApiDebugger\openapi-v1.yaml");

        foreach (var specFile in openApiSpecificationFiles)
        {
            var generatedSpecRelativePath = Path.GetRelativePath(this._openApiToolsDirectory, specFile);
            await this._processWrapper.RunProcessAsync(oasdiffExecutePath, new[] { "diff", baseSpecRelativePath, generatedSpecRelativePath }, cancellationToken);
        }
    }

    private async Task DecompressDownloadedFileAsync(string oasdiffFileName, CancellationToken cancellationToken)
    {
        var pathToCompressedFile = Path.Combine(this._oasdiffDirectory, oasdiffFileName);
        await this._processWrapper.RunProcessAsync("tar", new[] { "-xzf", $"{pathToCompressedFile}", "-C", $"{this._oasdiffDirectory}" }, cancellationToken);
    }

    private static string GetOasdiffFileName()
    {
        var osType = RuntimeInformationHelper.GetOperatingSystem();
        var architecture = RuntimeInformationHelper.GetArchitecture("amd");

        var fileName = $"oasdiff_{OasdiffVersion}_{osType}_{architecture}.tar.gz";

        if (osType == "macos")
        {
            fileName = $"oasdiff_{OasdiffVersion}_darwin_all.tar.gz";
        }

        return fileName;
    }
}