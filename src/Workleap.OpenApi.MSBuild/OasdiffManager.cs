using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class OasdiffManager : IOasdiffManager
{
    private const string OasdiffVersion = "1.9.2";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly string _openApiToolsDirectory;
    private readonly string _oasdiffDirectory;

    public OasdiffManager(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, string openApiToolsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
        this._processWrapper = processWrapper;
        this._openApiToolsDirectory = openApiToolsDirectoryPath;
        this._oasdiffDirectory = Path.Combine(openApiToolsDirectoryPath, "oasdiff", OasdiffVersion);
    }
    
    public async Task InstallOasdiffAsync(CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Starting Oasdiff installation.");
            
        Directory.CreateDirectory(this._oasdiffDirectory);

        var oasdiffFileName = GetOasdiffFileName();
        var url = $"https://github.com/Tufin/oasdiff/releases/download/v{OasdiffVersion}/{oasdiffFileName}";

        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, Path.Combine(this._oasdiffDirectory, oasdiffFileName), cancellationToken);
        await this.DecompressDownloadedFileAsync(oasdiffFileName, cancellationToken);
            
        this._loggerWrapper.LogMessage("Oasdiff installation completed.");
    }

    public async Task RunOasdiffAsync(IEnumerable<string> openApiSpecFiles, IEnumerable<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken)
    {
        var generatedOpenApiSpecFilesList = generatedOpenApiSpecFiles.ToList();
        var oasdiffExecutePath = Path.Combine(this._oasdiffDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "oasdiff.exe" : "oasdiff");

        foreach (var specFile in openApiSpecFiles)
        {
            this._loggerWrapper.LogMessage($"Starting Oasdiff comparison with {specFile}.");
            
            var fileName = Path.GetFileName(specFile);
            var baseSpecRelativePath = Path.GetRelativePath(this._openApiToolsDirectory, specFile);
            var newSpecRelativePath = Path.GetRelativePath(this._openApiToolsDirectory, generatedOpenApiSpecFilesList.First(x => x.Contains(fileName)));
            await this._processWrapper.RunProcessAsync(oasdiffExecutePath, new[] { "diff", baseSpecRelativePath, newSpecRelativePath, "--exclude-elements", "description,examples,title,summary", "-f", "text" }, cancellationToken);
        }
    }

    private async Task DecompressDownloadedFileAsync(string oasdiffFileName, CancellationToken cancellationToken)
    {
        var pathToCompressedFile = Path.Combine(this._oasdiffDirectory, oasdiffFileName);
        if (File.Exists(pathToCompressedFile))
        {
            return;
        }
        
        var exitCode = await this._processWrapper.RunProcessAsync("tar", new[] { "-xzf", $"{pathToCompressedFile}", "-C", $"{this._oasdiffDirectory}" }, cancellationToken);

        if (exitCode != 0)
        {
            throw new OpenApiTaskFailedException("Failed to decompress oasdiff.");
        }
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