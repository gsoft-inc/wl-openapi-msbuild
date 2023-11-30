using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

internal sealed class OasdiffManager : IOasdiffManager
{
    private const string OasdiffVersion = "1.9.6";
    private const string OasdiffDownloadUrlFormat = "https://github.com/Tufin/oasdiff/releases/download/v{0}/{1}";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly string _oasdiffDirectory;

    public OasdiffManager(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, string openApiToolsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
        this._processWrapper = processWrapper;
        this._oasdiffDirectory = Path.Combine(openApiToolsDirectoryPath, "oasdiff", OasdiffVersion);
    }
    
    public async Task InstallOasdiffAsync(CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Starting Oasdiff installation.");
            
        Directory.CreateDirectory(this._oasdiffDirectory);

        var oasdiffFileName = GetOasdiffFileName();
        var url = string.Format(OasdiffDownloadUrlFormat, OasdiffVersion, oasdiffFileName);

        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, Path.Combine(this._oasdiffDirectory, oasdiffFileName), cancellationToken);
        await this.DecompressDownloadedFileAsync(oasdiffFileName, cancellationToken);
            
        this._loggerWrapper.LogMessage("Oasdiff installation completed.");
    }

    public async Task RunOasdiffAsync(IEnumerable<string> openApiSpecFiles, IEnumerable<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken)
    {
        var generatedOpenApiSpecFilesList = generatedOpenApiSpecFiles.ToList();
        var oasdiffExecutePath = Path.Combine(this._oasdiffDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "oasdiff.exe" : "oasdiff");

        foreach (var baseSpecFile in openApiSpecFiles)
        {
            this._loggerWrapper.LogMessage($"\n ******** Oasdiff: Diff comparison with {baseSpecFile} ********", MessageImportance.High);
            
            var fileName = Path.GetFileName(baseSpecFile);
            var generatedSpecFilePath = generatedOpenApiSpecFilesList.FirstOrDefault(x => x.Contains(fileName));
            
            if (generatedSpecFilePath == null)
            {
                this._loggerWrapper.LogWarning($"Could not find a generated spec file for {baseSpecFile}.");
                continue;
            }
            
            var result = await this._processWrapper.RunProcessAsync(oasdiffExecutePath, new[] { "diff", baseSpecFile, generatedSpecFilePath, "--exclude-elements", "description,examples,title,summary", "-f", "text", "-o" }, cancellationToken);
            this._loggerWrapper.LogMessage(result.StandardOutput, MessageImportance.High);
            if (!string.IsNullOrEmpty(result.StandardError))
            {
                this._loggerWrapper.LogWarning(result.StandardError);
            }

            this._loggerWrapper.LogMessage($"\n ****************************************************************", MessageImportance.High);
        }
    }

    private async Task DecompressDownloadedFileAsync(string oasdiffFileName, CancellationToken cancellationToken)
    {
        var alreadyDecompressed = Path.Combine(this._oasdiffDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "oasdiff.exe" : "oasdiff");
        var pathToCompressedFile = Path.Combine(this._oasdiffDirectory, oasdiffFileName);
        if (File.Exists(alreadyDecompressed))
        {
            return;
        }
        
        var result = await this._processWrapper.RunProcessAsync("tar", new[] { "-xzf", $"{pathToCompressedFile}", "-C", $"{this._oasdiffDirectory}" }, cancellationToken);

        if (result.ExitCode != 0)
        {
            this._loggerWrapper.LogMessage(result.StandardOutput, MessageImportance.High);
            this._loggerWrapper.LogWarning(result.StandardError);
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