namespace Workleap.OpenApi.MSBuild;

public interface ISwaggerManager
{
    Task RunSwaggerAsync(string[] openApiSwaggerDocumentNames, CancellationToken cancellationToken);

    Task InstallSwaggerCliAsync(CancellationToken cancellationToken);

    Task GenerateOpenApiSpecAsync(string swaggerExePath, string outputOpenApiSpecPath, string documentName, CancellationToken cancellationToken);
}