using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.Buffered;
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
        this.Log.LogMessage(MessageImportance.High, "OpenApiWebApiAssemblyPath = '{0}'", this.OpenApiWebApiAssemblyPath);
        this.Log.LogMessage(MessageImportance.High, "OpenApiToolsDirectoryPath = '{0}'", this.OpenApiToolsDirectoryPath);
        this.Log.LogMessage(MessageImportance.High, "OpenApiSpectralRulesetUrl = '{0}'", this.OpenApiSpectralRulesetUrl);
        this.Log.LogMessage(MessageImportance.High, "OpenApiSwaggerDocumentNames = '{0}'", string.Join(", ", this.OpenApiSwaggerDocumentNames));
        this.Log.LogMessage(MessageImportance.High, "OpenApiSpecificationFiles = '{0}'", string.Join(", ", this.OpenApiSpecificationFiles));
        
        this.OpenApiSwaggerDocumentNames = this.OpenApiSwaggerDocumentNames is { Length: > 0 }
            ? new HashSet<string>(this.OpenApiSwaggerDocumentNames, StringComparer.Ordinal).ToArray()
            : new[] { "v1" };
        
        try
        {
            Directory.Delete(this.OpenApiToolsDirectoryPath, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        
        var swaggerDirPath = Path.Combine(this.OpenApiToolsDirectoryPath, "swagger");
        Directory.CreateDirectory(swaggerDirPath);
        
        await this.GeneratePublicNugetSource();

        // Install Swagger CLI
        await this.InstallSwaggerCliAsync(swaggerDirPath, cancellationToken);

        var swaggerExePath = Path.Combine(swaggerDirPath, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "swagger.exe" : "swagger");

        foreach (var documentName in this.OpenApiSwaggerDocumentNames)
        {
            var outputOpenApiSpecName = $"openapi-{documentName.ToLowerInvariant()}-{Guid.NewGuid().ToString("N").Substring(0, 6)}.yaml";
            var outputOpenApiSpecPath = Path.Combine(swaggerDirPath, outputOpenApiSpecName);
            await this.GenerateOpenApiSpecAsync(swaggerExePath, outputOpenApiSpecPath, documentName, cancellationToken);
        }

        // Install spectral

        // Install oasdiff
        return true;
    }

    private async Task GeneratePublicNugetSource()
    {
        if (!File.Exists(Path.Combine(this.OpenApiToolsDirectoryPath, "nuget.config")))
        {
            using var outputFile = new StreamWriter(Path.Combine(this.OpenApiToolsDirectoryPath, "nuget.config"), true);
            await outputFile.WriteLineAsync(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <clear />\n    <add key=\"nuget\" value=\"https://api.nuget.org/v3/index.json\" />\n  </packageSources>\n</configuration>");
        }
    }

    private async Task InstallSwaggerCliAsync(string swaggerDirPath, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        while (retryCount < 2)
        {
            var exitCode = await this.RunProcessAsync("dotnet", new[] { "tool", "update", "Swashbuckle.AspNetCore.Cli", "--tool-path", swaggerDirPath }, cancellationToken);

            if (exitCode != 0 && retryCount != 1)
            {
                this.Log.LogMessage(MessageImportance.High, "Swashbuckle download failed. Retrying once more...");
                retryCount++;
                continue;
            }

            break;
        }
    }

    private async Task GenerateOpenApiSpecAsync(string swaggerExePath, string outputOpenApiSpecPath, string documentName, CancellationToken cancellationToken)
        => await this.RunProcessAsync(swaggerExePath, new[] { "tofile", "--output", outputOpenApiSpecPath, "--yaml", this.OpenApiWebApiAssemblyPath, documentName }, cancellationToken);

    private async Task<int> RunProcessAsync(string filename, string[] arguments, CancellationToken cancellationToken)
    {
        var result = await Cli.Wrap(filename)
            .WithWorkingDirectory(this.OpenApiToolsDirectoryPath)
            .WithValidation(CommandResultValidation.None)
            .WithArguments(arguments)
            .ExecuteBufferedAsync(cancellationToken);

        this.Log.LogMessage(MessageImportance.High, "stdout = '{0}'", result.StandardOutput);
        this.Log.LogMessage(MessageImportance.High, "stderr = '{0}'", result.StandardError);
        this.Log.LogMessage(MessageImportance.High, "exit code = '{0}'", result.ExitCode);

        return result.ExitCode;
    }
}