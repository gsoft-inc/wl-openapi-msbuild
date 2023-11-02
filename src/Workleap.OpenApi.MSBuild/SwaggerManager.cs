using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SwaggerManager : ISwaggerManager
{
    private const string SwaggerVersion = "6.5.0";
    private readonly IProcessWrapper _processWrapper;
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly string _openApiWebApiAssemblyPath;
    private readonly string _swaggerDirectory;

    public SwaggerManager(IProcessWrapper processWrapper, ILoggerWrapper loggerWrapper, string openApiToolsDirectoryPath, string openApiWebApiAssemblyPath)
    {
        this._processWrapper = processWrapper;
        this._loggerWrapper = loggerWrapper;
        this._openApiWebApiAssemblyPath = openApiWebApiAssemblyPath;
        this._swaggerDirectory = Path.Combine(openApiToolsDirectoryPath, "swagger", SwaggerVersion);
    }

    public async Task RunSwaggerAsync(string[] openApiSwaggerDocumentNames, CancellationToken cancellationToken)
    {
        var swaggerExePath = Path.Combine(this._swaggerDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "swagger.exe" : "swagger");

        foreach (var documentName in openApiSwaggerDocumentNames)
        {
            var outputOpenApiSpecName = $"openapi-{documentName.ToLowerInvariant()}.yaml";

            var outputOpenApiSpecPath = Path.Combine(this._swaggerDirectory, outputOpenApiSpecName);

            await this.GenerateOpenApiSpecAsync(swaggerExePath, outputOpenApiSpecPath, documentName, cancellationToken);
        }
    }

    public async Task InstallSwaggerCliAsync(CancellationToken cancellationToken)
    {
        var retryCount = 0;
        while (retryCount < 2)
        {
            var exitCode = await this._processWrapper.RunProcessAsync("dotnet", new[] { "tool", "update", "Swashbuckle.AspNetCore.Cli", "--tool-path", this._swaggerDirectory, "--version", SwaggerVersion }, cancellationToken);

            if (exitCode != 0 && retryCount != 1)
            {
                this._loggerWrapper.Helper.LogWarning("Swashbuckle download failed. Retrying once more...");
                retryCount++;
                continue;
            }

            if (retryCount == 1 && exitCode != 0)
            {
                throw new OpenApiTaskFailedException("Swashbuckle CLI could not be installed.");
            }

            break;
        }
    }

    public async Task GenerateOpenApiSpecAsync(string swaggerExePath, string outputOpenApiSpecPath, string documentName, CancellationToken cancellationToken)
    {
        var exitCode = await this._processWrapper.RunProcessAsync(swaggerExePath, new[] { "tofile", "--output", outputOpenApiSpecPath, "--yaml", this._openApiWebApiAssemblyPath, documentName }, cancellationToken);

        if (exitCode != 0)
        {
            throw new OpenApiTaskFailedException("OpenApi file could not be created.");
        }
    }
}