using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Build.Framework;
using Workleap.OpenApi.MSBuild.Exceptions;

namespace Workleap.OpenApi.MSBuild;

public sealed class ValidateOpenApiTask : CancelableAsyncTask
{
    private ILoggerWrapper _loggerWrapper;
    private IProcessWrapper _processWrapper;
    private ISwaggerManager _swaggerManager;
    private ISpectralManager _spectralManager;

    public ValidateOpenApiTask(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, ISwaggerManager swaggerManager, ISpectralManager spectralManager)
    {
        this._loggerWrapper = loggerWrapper;
        this._processWrapper = processWrapper;
        this._swaggerManager = swaggerManager;
        this._spectralManager = spectralManager;
    }

    public ValidateOpenApiTask()
    {
        this._loggerWrapper = new LoggerWrapper(this.Log);
        this._processWrapper = new ProcessWrapper(this.OpenApiToolsDirectoryPath);
        this._swaggerManager = new SwaggerManager(this._processWrapper, this._loggerWrapper, this.OpenApiToolsDirectoryPath, this.OpenApiWebApiAssemblyPath);
        this._spectralManager = new SpectralManager(this._processWrapper, this._loggerWrapper, this.OpenApiToolsDirectoryPath);
    }

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
        this._loggerWrapper = new LoggerWrapper(this.Log);
        this._processWrapper = new ProcessWrapper(this.OpenApiToolsDirectoryPath);
        this._swaggerManager = new SwaggerManager(this._processWrapper, this._loggerWrapper, this.OpenApiToolsDirectoryPath, this.OpenApiToolsDirectoryPath);
        this._spectralManager = new SpectralManager(this._processWrapper, this._loggerWrapper, this.OpenApiToolsDirectoryPath);

        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiWebApiAssemblyPath), this.OpenApiWebApiAssemblyPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiToolsDirectoryPath), this.OpenApiToolsDirectoryPath);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpectralRulesetUrl), this.OpenApiSpectralRulesetUrl);
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSwaggerDocumentNames), string.Join(", ", this.OpenApiSwaggerDocumentNames));
        this.Log.LogMessage(MessageImportance.Low, "{0} = '{1}'", nameof(this.OpenApiSpecificationFiles), string.Join(", ", this.OpenApiSpecificationFiles));

        if (this.OpenApiSpecificationFiles.Length != this.OpenApiSwaggerDocumentNames.Length)
        {
            this.Log.LogWarning("OpenApiSpecificationFiles and OpenApiSwaggerDocumentNames should have the same lenght", this.OpenApiWebApiAssemblyPath);

            return false;
        }

        try
        {
            await this.GeneratePublicNugetSource();

            // Install Swagger CLI
            await this._swaggerManager.InstallSwaggerCliAsync(cancellationToken);

            // await this._swaggerManager.RunSwaggerAsync(this.OpenApiSwaggerDocumentNames, cancellationToken);

            // Install spectral
            await this._spectralManager.InstallSpectralAsync(cancellationToken);

            // Install oasdiff
        }
        catch (OpenApiTaskFailedException e)
        {
            this.Log.LogWarning("OpenApi validation could not be done. {0}", e.Message);
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