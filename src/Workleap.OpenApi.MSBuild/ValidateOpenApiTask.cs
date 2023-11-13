using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

public sealed class ValidateOpenApiTask : CancelableAsyncTask
{
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
        var loggerWrapper = new LoggerWrapper(this.Log);
        var processWrapper = new ProcessWrapper(this.OpenApiToolsDirectoryPath);
        var swaggerManager = new SwaggerManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, this.OpenApiWebApiAssemblyPath);

        using var httpClientWrapper = new HttpClientWrapper();

        var spectralManager = new SpectralManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, httpClientWrapper);
        var oasdiffManager = new OasdiffManager(loggerWrapper, processWrapper, this.OpenApiToolsDirectoryPath, httpClientWrapper);

        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiWebApiAssemblyPath), this.OpenApiWebApiAssemblyPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiToolsDirectoryPath), this.OpenApiToolsDirectoryPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpectralRulesetUrl), this.OpenApiSpectralRulesetUrl);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSwaggerDocumentNames), string.Join(", ", this.OpenApiSwaggerDocumentNames));
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpecificationFiles), string.Join(", ", this.OpenApiSpecificationFiles));

        if (this.OpenApiSpecificationFiles.Length != this.OpenApiSwaggerDocumentNames.Length)
        {
            this.Log.LogWarning("You must provide the same amount of open api specification file names and swagger document file names");

            return false;
        }

        try
        {
            await this.GeneratePublicNugetSource();

            var installSwaggerCliTask = swaggerManager.InstallSwaggerCliAsync(cancellationToken);
            var installSpectralTask = spectralManager.InstallSpectralAsync(cancellationToken);
            var installOasdiffTask = oasdiffManager.InstallOasdiffAsync(cancellationToken);
            
            await installSwaggerCliTask;
            await installSpectralTask;
            await installOasdiffTask;

            var generateOpenApiDocsPath = (await swaggerManager.RunSwaggerAsync(this.OpenApiSwaggerDocumentNames, cancellationToken)).ToList();
            var runSpectralTask = spectralManager.RunSpectralAsync(this.OpenApiSpecificationFiles, this.OpenApiSpectralRulesetUrl, cancellationToken);
            var runOasdiffTask = oasdiffManager.RunOasdiffAsync(this.OpenApiSpecificationFiles, generateOpenApiDocsPath, cancellationToken);

            await runSpectralTask;
            await runOasdiffTask;
        }
        catch (OpenApiTaskFailedException e)
        {
            this.Log.LogWarning("An error occurred while validating the OpenAPI specification: {0}", e.Message);
        }

        return true;
    }

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