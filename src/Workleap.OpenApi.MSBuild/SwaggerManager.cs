using System.Runtime.InteropServices;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SwaggerManager : ISwaggerManager
{
    private const string SwaggerVersion = "6.5.0";
    private readonly IProcessWrapper _processWrapper;
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly string _openApiWebApiAssemblyPath;
    private readonly string _swaggerDirectory;
    private readonly string _openApiToolsDirectoryPath;
    private readonly string _swaggerExecutablePath;

    public SwaggerManager(ILoggerWrapper loggerWrapper, IProcessWrapper processWrapper, string openApiToolsDirectoryPath, string openApiWebApiAssemblyPath)
    {
        this._processWrapper = processWrapper;
        this._loggerWrapper = loggerWrapper;
        this._openApiWebApiAssemblyPath = openApiWebApiAssemblyPath;
        this._openApiToolsDirectoryPath = openApiToolsDirectoryPath;
        this._swaggerDirectory = Path.Combine(openApiToolsDirectoryPath, "swagger", SwaggerVersion);
        this._swaggerExecutablePath = Path.Combine(this._swaggerDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "swagger.exe" : "swagger");
    }

    public async Task<IEnumerable<string>> RunSwaggerAsync(string[] openApiSwaggerDocumentNames, CancellationToken cancellationToken)
    {
        var taskList = new List<Task<string>>();

        foreach (var documentName in openApiSwaggerDocumentNames)
        {
            var outputOpenApiSpecName = $"openapi-{documentName.ToLowerInvariant()}.yaml";

            var outputOpenApiSpecPath = Path.Combine(this._openApiToolsDirectoryPath, outputOpenApiSpecName);

            taskList.Add(this.GenerateOpenApiSpecAsync(this._swaggerExecutablePath, outputOpenApiSpecPath, documentName, cancellationToken));
        }

        return await Task.WhenAll(taskList);
    }

    public async Task InstallSwaggerCliAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(this._swaggerExecutablePath))
        {
            return;
        }
        
        var retryCount = 0;
        while (retryCount < 2)
        {
            var exitCode = await this._processWrapper.RunProcessAsync("dotnet", new[] { "tool", "update", "Swashbuckle.AspNetCore.Cli", "--tool-path", this._swaggerDirectory, "--version", SwaggerVersion }, cancellationToken: cancellationToken);

            if (exitCode != 0 && retryCount != 1)
            {
                this._loggerWrapper.LogWarning("Swashbuckle download failed. Retrying once more...");
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

    public async Task<string> GenerateOpenApiSpecAsync(string swaggerExePath, string outputOpenApiSpecPath, string documentName, CancellationToken cancellationToken)
    {
        var exitCode = await this._processWrapper.RunProcessAsync(swaggerExePath, new[] { "tofile", "--output", outputOpenApiSpecPath, "--yaml", this._openApiWebApiAssemblyPath, documentName }, cancellationToken: cancellationToken);

        if (exitCode != 0)
        {
            throw new OpenApiTaskFailedException($"OpenApi file {outputOpenApiSpecPath} could not be created.");
        }

        return outputOpenApiSpecPath;
    }
}