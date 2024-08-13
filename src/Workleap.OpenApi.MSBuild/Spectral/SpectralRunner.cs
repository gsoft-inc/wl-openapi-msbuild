using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild.Spectral;

internal sealed class SpectralRunner
{
    private const string SpectralVersion = "6.11.0";

    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IProcessWrapper _processWrapper;
    private readonly DiffCalculator _diffCalculator;
    private readonly ICiReportRenderer _ciReportRenderer;
    private readonly string _spectralDirectory;
    private readonly string _openApiReportsDirectoryPath;
    
    public SpectralRunner(
        ILoggerWrapper loggerWrapper, 
        IProcessWrapper processWrapper, 
        DiffCalculator diffCalculator,
        ICiReportRenderer ciReportRenderer,
        string openApiToolsDirectoryPath, 
        string openApiReportsDirectoryPath)
    {
        this._loggerWrapper = loggerWrapper;
        this._openApiReportsDirectoryPath = openApiReportsDirectoryPath;
        this._spectralDirectory = Path.Combine(openApiToolsDirectoryPath, "spectral", SpectralVersion);
        this._processWrapper = processWrapper;
        this._diffCalculator = diffCalculator;
        this._ciReportRenderer = ciReportRenderer;
    }
    
    public async Task RunSpectralAsync(IReadOnlyCollection<string> openApiDocumentPaths, string spectralExecutablePath, string spectralRulesetPath, CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("\n ******** Spectral: Validating OpenAPI Documents against ruleset ********", MessageImportance.High);
        
        var shouldRunSpectral = await this.ShouldRunSpectral(spectralRulesetPath, openApiDocumentPaths);
        if (!shouldRunSpectral)
        {
            this._loggerWrapper.LogMessage("\n=> Spectral step skipped since the OpenAPI document and ruleset have not changed.", MessageImportance.High);
            foreach (var documentPath in openApiDocumentPaths)
            {
                this._loggerWrapper.LogMessage("- Check previous report here: {0}", MessageImportance.High, messageArgs: this.GetReportPath(documentPath));
            }
            
            this._loggerWrapper.LogMessage("\n ****************************************************************", MessageImportance.High);
            return;
        }

        this._loggerWrapper.LogMessage("Starting Spectral report generation.");
        var spectralExecutePath = Path.Combine(this._spectralDirectory, spectralExecutablePath);

        foreach (var documentPath in openApiDocumentPaths)
        {
            var documentName = Path.GetFileNameWithoutExtension(documentPath);
            var spectralReportPath = this.GetReportPath(documentPath);
            
            this._loggerWrapper.LogMessage("\n *** Spectral: Validating {0} against ruleset ***", MessageImportance.High, documentName);
            this._loggerWrapper.LogMessage("- File path: {0}", MessageImportance.High, documentPath);
            this._loggerWrapper.LogMessage("- Ruleset : {0}\n", MessageImportance.High, spectralRulesetPath);

            if (File.Exists(spectralReportPath))
            {
                this._loggerWrapper.LogMessage("\nDeleting existing report: {0}", messageArgs: spectralReportPath);
                File.Delete(spectralReportPath);
            }
            
            await this.GenerateSpectralReport(spectralExecutePath, documentPath, spectralRulesetPath, spectralReportPath, cancellationToken);
            await this._ciReportRenderer.AttachReportToBuildAsync(spectralReportPath);
            this._loggerWrapper.LogMessage("\n ****************************************************************", MessageImportance.High);
        }
        
        this._diffCalculator.SaveCurrentExecutionChecksum(spectralRulesetPath, openApiDocumentPaths);
    }
    
    private async Task<bool> ShouldRunSpectral(string spectralRulesetPath, IReadOnlyCollection<string> openApiDocumentPaths)
    {
        if (this._diffCalculator.HasRulesetChangedSinceLastExecution(spectralRulesetPath))
        {
            return true;
        }

        if (this._diffCalculator.HasOpenApiDocumentChangedSinceLastExecution(openApiDocumentPaths))
        {
            return true;
        }

        return false;
    }
    
    private string GetReportPath(string documentPath)
    {
        var documentName = Path.GetFileNameWithoutExtension(documentPath);
        var outputSpectralReportName = $"spectral-{documentName}.txt";
        return Path.Combine(this._openApiReportsDirectoryPath, outputSpectralReportName);
    }

    private async Task GenerateSpectralReport(string spectralExecutePath, string swaggerDocumentPath, string rulesetPath, string spectralReportPath, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            this._loggerWrapper.LogMessage("Granting execute permission to {0}", MessageImportance.Normal, spectralExecutePath);
            await this.AssignExecutePermission(spectralExecutePath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Running Spectral...", MessageImportance.Normal);
        var result = await this._processWrapper.RunProcessAsync(spectralExecutePath, new[] { "lint", swaggerDocumentPath, "--ruleset", rulesetPath, "--format", "pretty", "--format", "stylish", "--output.stylish", spectralReportPath, "--fail-severity=warn", "--verbose" }, cancellationToken);
        
        this._loggerWrapper.LogMessage(result.StandardOutput, MessageImportance.High);
        if (!string.IsNullOrEmpty(result.StandardError))
        {
            this._loggerWrapper.LogWarning(result.StandardError);
        }
        
        if (!File.Exists(spectralReportPath))
        {
            throw new OpenApiTaskFailedException($"Spectral report for {swaggerDocumentPath} could not be created. Please check the CONSOLE output above for more details.");
        }

        if (result.ExitCode != 0)
        {
            this._loggerWrapper.LogWarning($"Spectral scan detected violation of ruleset. Please check the report [{spectralReportPath}] for more details.");
        }

        this._loggerWrapper.LogMessage("Spectral report generated. {0}", messageArgs: spectralReportPath);
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