namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a Code First approach it will:
///     1. (if not disabled) Generate the OpenAPI specification files from the code
///     2. Validate the OpenAPI specification files base on spectral rules 
/// </summary>
internal class CodeFirstProcess
{
    private const string DisableSpecGenEnvVarName = "WL_DISABLE_SPECGEN";
    
    private readonly SpectralManager _spectralManager;
    private readonly SwaggerManager _swaggerManager;
    private readonly SpecGeneratorManager _specGeneratorManager;

    internal CodeFirstProcess(SpectralManager spectralManager, SwaggerManager swaggerManager, SpecGeneratorManager specGeneratorManager)
    {
        this._spectralManager = spectralManager;
        this._swaggerManager = swaggerManager;
        this._specGeneratorManager = specGeneratorManager;
    }
    
    internal async Task Execute(
        string[] openApiSpecificationFilesPath,
        string[] openApiSwaggerDocumentNames,
        string openApiSpectralRulesetUrl,
        CancellationToken cancellationToken)
    {
        var isGenerationEnabled = !string.Equals(Environment.GetEnvironmentVariable(DisableSpecGenEnvVarName), "true", StringComparison.OrdinalIgnoreCase);

        await this.InstallDependencies(isGenerationEnabled, cancellationToken);
        
        if (isGenerationEnabled)
        {
            var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();
            await this._specGeneratorManager.UpdateSpecificationFilesAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        }

        await this._spectralManager.RunSpectralAsync(openApiSpecificationFilesPath, openApiSpectralRulesetUrl, cancellationToken);
    }

    private async Task InstallDependencies(
        bool isGenerationEnable,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        installationTasks.Add(this._spectralManager.InstallSpectralAsync(cancellationToken));        

        if (isGenerationEnable)
        {
            installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
    }
}