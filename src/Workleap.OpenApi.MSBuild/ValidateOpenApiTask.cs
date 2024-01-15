using System.Diagnostics;
using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

public sealed class ValidateOpenApiTask : CancelableAsyncTask
{
    private const string CodeFirst = "CodeFirst";
    private const string ContractFirst = "ContractFirst";
    
    /// <summary>
    /// 2 supported modes:
    ///     - CodeFirst: Generate the OpenAPI specification files from the code
    ///     - ContractFirst: Will use the OpenAPI specification files provided
    /// </summary>
    [Required]
    public string OpenApiDevelopmentMode { get; set; } = string.Empty;

    /// <summary>When Development mode is Contract first, will validate if the specification match the code.</summary>  
    [Required]
    public bool OpenApiCompareCodeAgainstSpecFile { get; set; } = false;
    
    /// <summary>The path of the ASP.NET Core project startup assembly directory.</summary>
    [Required]
    public string StartupAssemblyPath { get; set; } = string.Empty;
    
    /// <summary>The path of the ASP.NET Core project being built.</summary>
    [Required]
    public string OpenApiWebApiAssemblyPath { get; set; } = string.Empty;

    /// <summary>The base directory path where the OpenAPI tools will be downloaded.</summary>
    [Required]
    public string OpenApiToolsDirectoryPath { get; set; } = string.Empty;

    /// <summary>The URL of the OpenAPI Spectral ruleset to validate against.</summary>
    [Required]
    public string OpenApiSpectralRulesetUrl { get; set; } = string.Empty;

    /// <summary>The names of the Swagger documents to generate OpenAPI specifications for.</summary>
    [Required]
    public string[] OpenApiSwaggerDocumentNames { get; set; } = Array.Empty<string>();

    /// <summary>The paths of the OpenAPI specification files to validate against.</summary>
    [Required]
    public string[] OpenApiSpecificationFiles { get; set; } = Array.Empty<string>();

    protected override async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        var reportsPath = Path.Combine(this.OpenApiToolsDirectoryPath, "reports");
        var loggerWrapper = new LoggerWrapper(this.Log);
        var processWrapper = new ProcessWrapper(this.StartupAssemblyPath);
        var swaggerManager = new SwaggerManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, this.OpenApiWebApiAssemblyPath);

        using var httpClientWrapper = new HttpClientWrapper();

        var spectralManager = new SpectralManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, reportsPath, httpClientWrapper);
        var oasdiffManager = new OasdiffManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, httpClientWrapper);
        var specGeneratorManager = new SpecGeneratorManager(loggerWrapper); 

        var codeFirstProcess = new CodeFirstProcess(spectralManager, swaggerManager, specGeneratorManager, oasdiffManager);
        var contractFirstProcess = new ContractFirstProcess(loggerWrapper, spectralManager, swaggerManager, oasdiffManager);

        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiWebApiAssemblyPath), this.OpenApiWebApiAssemblyPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiToolsDirectoryPath), this.OpenApiToolsDirectoryPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpectralRulesetUrl), this.OpenApiSpectralRulesetUrl);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSwaggerDocumentNames), string.Join(", ", this.OpenApiSwaggerDocumentNames));
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpecificationFiles), string.Join(", ", this.OpenApiSpecificationFiles));

        if (this.OpenApiSpecificationFiles.Length != this.OpenApiSwaggerDocumentNames.Length)
        {
            this.Log.LogWarning("You must provide the same amount of open api specification file names and swagger document file names.");

            return false;
        }

        try
        {
            await this.GeneratePublicNugetSource();
            Directory.CreateDirectory(reportsPath);

            switch (this.OpenApiDevelopmentMode)
            {
                case CodeFirst:
                    await codeFirstProcess.Execute(
                        this.OpenApiSpecificationFiles,
                        this.OpenApiSwaggerDocumentNames,
                        this.OpenApiSpectralRulesetUrl,
                        this.OpenApiCompareCodeAgainstSpecFile ? CodeFirstProcess.CodeFirstMode.SpecComparison : CodeFirstProcess.CodeFirstMode.SpecGeneration,
                        cancellationToken);
                    break;
                
                case ContractFirst:
                    var isSuccess = await contractFirstProcess.Execute(
                        this.OpenApiSpecificationFiles,
                        this.OpenApiToolsDirectoryPath,
                        this.OpenApiSwaggerDocumentNames,
                        this.OpenApiSpectralRulesetUrl,
                        this.OpenApiCompareCodeAgainstSpecFile ? ContractFirstProcess.CompareCodeAgainstSpecFile.Enabled : ContractFirstProcess.CompareCodeAgainstSpecFile.Disabled,
                        cancellationToken);

                    if (!isSuccess)
                    {
                        return false;
                    }

                    break;
                
                default:
                    this.Log.LogError("Invalid value of '{0}' for {1}. Allowed values are '{2}' or '{3}'", this.OpenApiDevelopmentMode, nameof(ValidateOpenApiTask.OpenApiDevelopmentMode), ContractFirst, CodeFirst);
                    return false;
            }
        }
        catch (OpenApiTaskFailedException e)
        {
            this.Log.LogWarning("An error occurred while validating the OpenAPI specification: {0}", e.Message);
        }

        return true;
    }

    // To avoid depending on the client feeds, which can can be private, we will exclusively use the public nuget feed.
    private async Task GeneratePublicNugetSource()
    {
        Directory.CreateDirectory(this.OpenApiToolsDirectoryPath);

        if (!File.Exists(Path.Combine(this.OpenApiToolsDirectoryPath, "nuget.config")))
        {
            var path = Path.Combine(this.OpenApiToolsDirectoryPath, "nuget.config");
            File.WriteAllText(path, "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <clear />\n    <add key=\"nuget\" value=\"https://api.nuget.org/v3/index.json\" />\n  </packageSources>\n</configuration>");
        }
    }
}