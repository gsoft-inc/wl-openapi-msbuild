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

    protected override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        this.Log.LogMessage(MessageImportance.High, "OpenApiWebApiAssemblyPath = '{0}'", this.OpenApiWebApiAssemblyPath);
        this.Log.LogMessage(MessageImportance.High, "OpenApiToolsDirectoryPath = '{0}'", this.OpenApiToolsDirectoryPath);
        this.Log.LogMessage(MessageImportance.High, "OpenApiSpectralRulesetUrl = '{0}'", this.OpenApiSpectralRulesetUrl);
        this.Log.LogMessage(MessageImportance.High, "OpenApiSwaggerDocumentNames = '{0}'", string.Join(", ", this.OpenApiSwaggerDocumentNames));
        this.Log.LogMessage(MessageImportance.High, "OpenApiSpecificationFiles = '{0}'", string.Join(", ", this.OpenApiSpecificationFiles));

        return Task.FromResult(true);
    }
}