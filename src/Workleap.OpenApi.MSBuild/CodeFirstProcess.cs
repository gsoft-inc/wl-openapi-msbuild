namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a Code First approach it will:
///     1. Generate the OpenAPI specification files from the code
///     2. Depending of <see cref="CodeFirstMode"/> it will either will compare the generated OpenAPI specification files against the provided specifications or it will update the source-controlled specification files 
///     2. Validate the OpenAPI specification files base on spectral rules 
/// </summary>
internal class CodeFirstProcess
{
    private readonly SpectralManager _spectralManager;
    private readonly SwaggerManager _swaggerManager;
    private readonly SpecGeneratorManager _specGeneratorManager;
    private readonly OasdiffManager _oasdiffManager;

    internal CodeFirstProcess(SpectralManager spectralManager, SwaggerManager swaggerManager, SpecGeneratorManager specGeneratorManager, OasdiffManager oasdiffManager)
    {
        this._spectralManager = spectralManager;
        this._swaggerManager = swaggerManager;
        this._specGeneratorManager = specGeneratorManager;
        this._oasdiffManager = oasdiffManager;
    }
    
    internal enum CodeFirstMode
    {
        SpecGeneration,
        SpecComparison,
    }
    
    internal async Task Execute(
        string[] openApiSpecificationFilesPath,
        string[] openApiSwaggerDocumentNames,
        string openApiSpectralRulesetUrl,
        CodeFirstMode mode,
        CancellationToken cancellationToken)
    {
        await this.InstallDependencies(mode, cancellationToken);
        
        var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();

        if (mode == CodeFirstMode.SpecGeneration)
        {
            await this._specGeneratorManager.UpdateSpecificationFilesAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        } 
        else
        {
            await this._oasdiffManager.RunOasdiffAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        }

        await this._spectralManager.RunSpectralAsync(openApiSpecificationFilesPath, openApiSpectralRulesetUrl, cancellationToken);
    }

    private async Task InstallDependencies(
        CodeFirstMode mode,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        installationTasks.Add(this._spectralManager.InstallSpectralAsync(cancellationToken));        
        installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
        
        if (mode == CodeFirstMode.SpecComparison)
        {
            installationTasks.Add(this._oasdiffManager.InstallOasdiffAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
    }
}