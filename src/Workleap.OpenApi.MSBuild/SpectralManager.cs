using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralManager : ISpectralManager
{
    private const string SpectralVersion = "6.11.0";
    private const string SpectralDownloadUrlFormat = "https://github.com/stoplightio/spectral/releases/download/v{0}/{1}";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _spectralDirectory;
    private readonly string _openApiReportsDirectoryPath;
    private readonly IProcessWrapper _processWrapper;
    
    public SpectralManager(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, string openApiToolsDirectoryPath, string openApiReportsDirectoryPath, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
        this._openApiReportsDirectoryPath = openApiReportsDirectoryPath;
        this._spectralDirectory = Path.Combine(openApiToolsDirectoryPath, "spectral", SpectralVersion);
        this._processWrapper = processWrapper;
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
            
        this._loggerWrapper.LogMessage("Spectral installation completed.");
    }

    public async Task RunSpectralAsync(IEnumerable<string> swaggerDocumentPaths, string rulesetUrl, CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Starting Spectral report generation.");
        
        var spectralExecutePath = Path.Combine(this._spectralDirectory, this.ExecutablePath);

        foreach (var documentPath in swaggerDocumentPaths)
        {
            var documentName = Path.GetFileNameWithoutExtension(documentPath);
            var outputSpectralReportName = $"spectral-{documentName}.html";
            await this.GenerateSpectralReport(spectralExecutePath, documentPath, rulesetUrl, Path.Combine(this._openApiReportsDirectoryPath, outputSpectralReportName), cancellationToken);
        }
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

    private async Task GenerateSpectralReport(string spectralExecutePath, string swaggerDocumentPath, string rulesetUrl, string htmlReportPath, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await this.AssignExecutePermission(spectralExecutePath, cancellationToken);
        }

        var exitCode = await this._processWrapper.RunProcessAsync(spectralExecutePath, new[] { "lint", swaggerDocumentPath, "--ruleset", rulesetUrl, "--format", "html", "--format", "pretty", "--output.html", htmlReportPath, "--fail-severity=warn" }, cancellationToken);
        if (exitCode != 0)
        {
            throw new OpenApiTaskFailedException($"Spectral report for {swaggerDocumentPath} could not be created.");
        }

        this._loggerWrapper.LogMessage("Spectral report generated. {0}", htmlReportPath);
    }

    private async Task AssignExecutePermission(string spectralExecutePath, CancellationToken cancellationToken)
    {
        var chmodExitCode = await this._processWrapper.RunProcessAsync("chmod", new[] { "+x",  spectralExecutePath }, cancellationToken);
        if (chmodExitCode != 0)
        {
            throw new OpenApiTaskFailedException($"Failed to provide execute permission to {spectralExecutePath}");
        }
    }
}