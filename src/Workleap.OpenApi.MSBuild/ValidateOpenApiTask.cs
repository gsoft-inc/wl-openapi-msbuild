using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

public sealed class ValidateOpenApiTask : CancelableAsyncTask
{
    private const string CodeFirst = "CodeFirst";
    private const string ContractFirst = "ContractFirst";

    /// <summary>
    ///     2 supported modes:
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

    /// <summary>If should log error instead of warning</summary>
    public bool OpenApiTreatWarningsAsErrors { get; set; }

    protected override async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        var loggerWrapper = new LoggerWrapper(this.Log, this.OpenApiTreatWarningsAsErrors);

        loggerWrapper.LogMessage("\n******** Starting {0} ********\n", MessageImportance.Normal, nameof(ValidateOpenApiTask));

        var reportsPath = Path.Combine(this.OpenApiToolsDirectoryPath, "reports");
        var processWrapper = new ProcessWrapper(this.StartupAssemblyPath);
        var swaggerManager = new SwaggerManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, this.OpenApiWebApiAssemblyPath);

        var httpClientWrapper = new HttpClientWrapper();

        var spectralManager = new SpectralManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, reportsPath, httpClientWrapper);
        var oasdiffManager = new OasdiffManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, httpClientWrapper);
        var specGeneratorManager = new SpecGeneratorManager(loggerWrapper);

        var codeFirstProcess = new CodeFirstProcess(loggerWrapper, spectralManager, swaggerManager, specGeneratorManager, oasdiffManager);
        var contractFirstProcess = new ContractFirstProcess(loggerWrapper, spectralManager, swaggerManager, oasdiffManager);

        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Normal, nameof(this.OpenApiDevelopmentMode), this.OpenApiDevelopmentMode);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Normal, nameof(this.OpenApiCompareCodeAgainstSpecFile), this.OpenApiCompareCodeAgainstSpecFile);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiTreatWarningsAsErrors), this.OpenApiTreatWarningsAsErrors);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiWebApiAssemblyPath), this.OpenApiWebApiAssemblyPath);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiToolsDirectoryPath), this.OpenApiToolsDirectoryPath);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiSpectralRulesetUrl), this.OpenApiSpectralRulesetUrl);
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiSwaggerDocumentNames), string.Join(", ", this.OpenApiSwaggerDocumentNames));
        loggerWrapper.LogMessage("{0} = '{1}'", MessageImportance.Low, nameof(this.OpenApiSpecificationFiles), string.Join(", ", this.OpenApiSpecificationFiles));

        if (this.OpenApiSpecificationFiles.Length != this.OpenApiSwaggerDocumentNames.Length)
        {
            loggerWrapper.LogWarning("You must provide the same amount of open api specification file names and swagger document file names.");

            return false;
        }

        try
        {
            await this.GeneratePublicNugetSource();
            Directory.CreateDirectory(reportsPath);

            switch (this.OpenApiDevelopmentMode)
            {
                case CodeFirst:
                    loggerWrapper.LogMessage("\nStarting code first...", MessageImportance.Normal);
                    await codeFirstProcess.Execute(
                        this.OpenApiSpecificationFiles,
                        this.OpenApiSwaggerDocumentNames,
                        this.OpenApiSpectralRulesetUrl,
                        this.OpenApiCompareCodeAgainstSpecFile ? CodeFirstProcess.CodeFirstMode.SpecComparison : CodeFirstProcess.CodeFirstMode.SpecGeneration,
                        cancellationToken);
                    break;

                case ContractFirst:
                    loggerWrapper.LogMessage("\nStarting contract first...", MessageImportance.Normal);
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
                    loggerWrapper.LogError("Invalid value of '{0}' for {1}. Allowed values are '{2}' or '{3}'", this.OpenApiDevelopmentMode, nameof(this.OpenApiDevelopmentMode), ContractFirst, CodeFirst);
                    return false;
            }
        }
        catch (OpenApiTaskFailedException e)
        {
            loggerWrapper.LogWarning("An error occurred while validating the OpenAPI specification: {0}", e.Message);
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