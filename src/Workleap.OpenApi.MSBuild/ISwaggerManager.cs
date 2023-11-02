namespace Workleap.OpenApi.MSBuild;

public interface ISwaggerManager
{
    Task<IEnumerable<string>> RunSwaggerAsync(string[] openApiSwaggerDocumentNames, CancellationToken cancellationToken);

    Task InstallSwaggerCliAsync(CancellationToken cancellationToken);

    Task<string> GenerateOpenApiSpecAsync(string swaggerExePath, string outputOpenApiSpecPath, string documentName, CancellationToken cancellationToken);
}