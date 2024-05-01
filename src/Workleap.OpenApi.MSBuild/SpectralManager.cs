using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralManager : ISpectralManager
{
    private const string SpectralVersion = "6.11.0";
    private const string SpectralDownloadUrlFormat = "https://github.com/stoplightio/spectral/releases/download/v{0}/{1}";
    private const string SpectralRulesetChecksumFileName = "spectral-ruleset-checksum.txt";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _spectralDirectory;
    private readonly string _openApiReportsDirectoryPath;
    private readonly IProcessWrapper _processWrapper;
    
    private string _spectralRulesetPath;
    
    public SpectralManager(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, string openApiToolsDirectoryPath, string openApiReportsDirectoryPath, string rulesetUrl, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
        this._openApiReportsDirectoryPath = openApiReportsDirectoryPath;
        this._spectralDirectory = Path.Combine(openApiToolsDirectoryPath, "spectral", SpectralVersion);
        this._processWrapper = processWrapper;
        this._spectralRulesetPath = rulesetUrl;
    }
    
    private string ExecutablePath { get; set; } = string.Empty;
    
    public async Task InstallSpectralAsync(CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Starting Spectral installation.");
            
        Directory.CreateDirectory(this._spectralDirectory);

        this.ExecutablePath = GetSpectralFileName();
        var url = string.Format(SpectralDownloadUrlFormat,  SpectralVersion, this.ExecutablePath);
        var destination = Path.Combine(this._spectralDirectory, this.ExecutablePath);
        
        await this._httpClientWrapper.DownloadFileToDestinationAsync(url, destination, cancellationToken);

        if (!IsLocalFile(this._spectralRulesetPath))
        {
            // We download the file before to isolate flakiness in the spectral execution
            this._loggerWrapper.LogMessage("Downloading ruleset.");
            this._spectralRulesetPath = await this.DownloadFileAsync(this._spectralRulesetPath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Spectral installation completed.");
    }

    public async Task RunSpectralAsync(IReadOnlyCollection<string> generatedOpenApiDocumentPaths, IReadOnlyCollection<string> sourcedControlOpenApiDocumentPaths, CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("\n ******** Spectral: Validating OpenApi Documents against ruleset ********", MessageImportance.High);
        
        var shouldRunSpectral = await this.ShouldRunSpectral(generatedOpenApiDocumentPaths, sourcedControlOpenApiDocumentPaths);
        if (!shouldRunSpectral)
        {
            this._loggerWrapper.LogMessage("\n=> Spectral step skipped since the OpenAPI document and ruleset have not changed.", MessageImportance.High);
            foreach (var documentPath in generatedOpenApiDocumentPaths)
            {
                this._loggerWrapper.LogMessage("- Check previous report here: {0}", MessageImportance.High, messageArgs: this.GetReportPath(documentPath));
            }
            
            this._loggerWrapper.LogMessage("\n ****************************************************************", MessageImportance.High);
            return;
        }

        this._loggerWrapper.LogMessage("Starting Spectral report generation.");
        var spectralExecutePath = Path.Combine(this._spectralDirectory, this.ExecutablePath);

        foreach (var documentPath in generatedOpenApiDocumentPaths)
        {
            var documentName = Path.GetFileNameWithoutExtension(documentPath);
            var htmlReportPath = this.GetReportPath(documentPath);
            
            this._loggerWrapper.LogMessage("\n *** Spectral: Validating {0} against ruleset ***", MessageImportance.High, documentName);
            this._loggerWrapper.LogMessage("- File path: {0}", MessageImportance.High, documentPath);
            this._loggerWrapper.LogMessage("- Ruleset : {0}\n", MessageImportance.High, this._spectralRulesetPath);

            if (File.Exists(htmlReportPath))
            {
                this._loggerWrapper.LogMessage("\nDeleting existing report: {0}", messageArgs: htmlReportPath);
                File.Delete(htmlReportPath);
            }
            
            await this.GenerateSpectralReport(spectralExecutePath, documentPath, this._spectralRulesetPath, htmlReportPath, cancellationToken);
            this._loggerWrapper.LogMessage("\n ****************************************************************", MessageImportance.High);
        }
        
        var rulesChecksum = GetChecksum(this._spectralRulesetPath);
        this.UpdateSpectralRulesetChecksum(rulesChecksum);
    }
    
    private async Task<bool> ShouldRunSpectral(IReadOnlyCollection<string> generatedDocumentPaths, IReadOnlyCollection<string> openApiSpecificationFilesPath)
    {
        if (this.HasRulesetChangedSinceLastExecution())
        {
            return true;
        }

        if (HasOpenApiDocumentChangedSinceLastExecution(generatedDocumentPaths, openApiSpecificationFilesPath))
        {
            return true;
        }

        return false;
    }

    private bool HasRulesetChangedSinceLastExecution()
    {
        var preciousRulesetChecksum = this.GetSpectralRulesetChecksum();
        var currentRulesetChecksum = GetChecksum(this._spectralRulesetPath);

        var hasRulesetChanged = !string.Equals(preciousRulesetChecksum, currentRulesetChecksum, StringComparison.InvariantCultureIgnoreCase); 

        return hasRulesetChanged;
    }
    
    private static bool HasOpenApiDocumentChangedSinceLastExecution(IReadOnlyCollection<string> generatedDocumentPaths, IReadOnlyCollection<string> openApiSpecificationFilesPath)
    {
        var existingDocumentChecksums = new Dictionary<string, string>();
        foreach (var filePath in openApiSpecificationFilesPath)
        {
            existingDocumentChecksums.Add(Path.GetFileNameWithoutExtension(filePath), GetChecksum(filePath));
        }
        
        var generatedDocumentChecksums = new Dictionary<string, string>();
        foreach (var filePath in generatedDocumentPaths)
        {
            generatedDocumentChecksums.Add(Path.GetFileNameWithoutExtension(filePath), GetChecksum(filePath));
        }

        foreach (var generatedFileCheksum in generatedDocumentChecksums)
        {
            if (existingDocumentChecksums.TryGetValue(generatedFileCheksum.Key, out var previousChecksum))
            {
                if (!string.Equals(previousChecksum, generatedFileCheksum.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static string GetChecksum(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
    
    private void UpdateSpectralRulesetChecksum(string checksum)
    {
        File.WriteAllText(Path.Combine(this._openApiReportsDirectoryPath, SpectralRulesetChecksumFileName), checksum);
    }
    
    private string GetSpectralRulesetChecksum()
    {
        var checksumFilePath = Path.Combine(this._openApiReportsDirectoryPath, SpectralRulesetChecksumFileName);
        if (!File.Exists(checksumFilePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(checksumFilePath);
    }

    private static string GetSpectralFileName()
    {
        var osType = RuntimeInformationHelper.GetOperatingSystem();
        var architecture = RuntimeInformationHelper.GetArchitecture();

        if (osType == "linux")
        {
            var distro = File.Exists("/etc/os-release") ? File.ReadAllText("/etc/os-release") : string.Empty;
            if (distro.Contains("Alpine Linux"))
            {
                osType = "alpine";
            }
        }

        var fileName = $"spectral-{osType}-{architecture}";

        if (osType == "windows")
        {
            fileName = "spectral.exe";
        }

        return fileName;
    }
    
    private string GetReportPath(string documentPath)
    {
        var documentName = Path.GetFileNameWithoutExtension(documentPath);
        var outputSpectralReportName = $"spectral-{documentName}.html";
        return Path.Combine(this._openApiReportsDirectoryPath, outputSpectralReportName);
    }
    
    private async Task<string> DownloadFileAsync(string rulesetUrl, CancellationToken cancellationToken)
    {
        try
        {
            this._loggerWrapper.LogMessage("Downloading rule file {0}", MessageImportance.Normal, rulesetUrl);
            
            var outputFilePath = Path.ChangeExtension(Path.GetTempFileName(), "yaml");
            await this._httpClientWrapper.DownloadFileToDestinationAsync(rulesetUrl, outputFilePath, cancellationToken);
            
            this._loggerWrapper.LogMessage("Download completed", MessageImportance.Normal);

            return outputFilePath;
        }
        catch (Exception e)
        {
            this._loggerWrapper.LogWarning(e.Message);
            throw new OpenApiTaskFailedException($"Failed to download {rulesetUrl}.");
        }
    }
    
    private static bool IsLocalFile(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.IsFile;
        }
        
        return true;
    }

    private async Task GenerateSpectralReport(string spectralExecutePath, string swaggerDocumentPath, string rulesetPath, string htmlReportPath, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            this._loggerWrapper.LogMessage("Granting execute permission to {0}", MessageImportance.Normal, spectralExecutePath);
            await this.AssignExecutePermission(spectralExecutePath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Running Spectral...", MessageImportance.Normal);
        var result = await this._processWrapper.RunProcessAsync(spectralExecutePath, new[] { "lint", swaggerDocumentPath, "--ruleset", rulesetPath, "--format", "html", "--format", "pretty", "--output.html", htmlReportPath, "--fail-severity=warn", "--verbose" }, cancellationToken);
        
        this._loggerWrapper.LogMessage(result.StandardOutput, MessageImportance.High);
        if (!string.IsNullOrEmpty(result.StandardError))
        {
            this._loggerWrapper.LogWarning(result.StandardError);
        }
        
        if (!File.Exists(htmlReportPath))
        {
            throw new OpenApiTaskFailedException($"Spectral report for {swaggerDocumentPath} could not be created. Please check the CONSOLE output above for more details.");
        }

        if (result.ExitCode != 0)
        {
            this._loggerWrapper.LogWarning($"Spectral scan detected violation of ruleset. Please check the report [{htmlReportPath}] for more details.");
        }

        this._loggerWrapper.LogMessage("Spectral report generated. {0}", messageArgs: htmlReportPath);
    }

    private async Task AssignExecutePermission(string spectralExecutePath, CancellationToken cancellationToken)
    {
        var result = await this._processWrapper.RunProcessAsync("chmod", new[] { "+x",  spectralExecutePath }, cancellationToken);
        if (result.ExitCode != 0)
        {
            this._loggerWrapper.LogMessage(result.StandardOutput, MessageImportance.High);
            this._loggerWrapper.LogWarning(result.StandardError);
            throw new OpenApiTaskFailedException($"Failed to provide execute permission to {spectralExecutePath}");
        }
    }
}