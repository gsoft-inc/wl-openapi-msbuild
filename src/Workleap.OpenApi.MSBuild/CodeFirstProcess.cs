namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a Code First approach it will:
///     1. Generate the OpenAPI specification files from the code
///     2. If <see cref="CompareCodeAgainstSpecFile"/> is enabled, will compare the generated OpenAPI specification files against the provided specifications otherwise will update the source-controlled specification files 
///     2. Validate the OpenAPI specification files base on spectral rules 
/// </summary>
internal class CodeFirstProcess
{
    private const string CompareSpecFlagEnvVarName = "WL_COMPARE_SPEC";
    
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
    
    internal async Task Execute(
        string[] openApiSpecificationFilesPath,
        string[] openApiSwaggerDocumentNames,
        string openApiSpectralRulesetUrl,
        CancellationToken cancellationToken)
    {
        var shouldCompareSpec = string.Equals(Environment.GetEnvironmentVariable(CompareSpecFlagEnvVarName), "true", StringComparison.OrdinalIgnoreCase);

        await this.InstallDependencies(shouldCompareSpec, cancellationToken);
        
        var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();

        if (shouldCompareSpec)
        {
            await this._oasdiffManager.RunOasdiffAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        } 
        else
        {
            await this._specGeneratorManager.UpdateSpecificationFilesAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        }

        await this._spectralManager.RunSpectralAsync(openApiSpecificationFilesPath, openApiSpectralRulesetUrl, cancellationToken);
    }

    private async Task InstallDependencies(
        bool shouldCompareSpec,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        installationTasks.Add(this._spectralManager.InstallSpectralAsync(cancellationToken));        
        installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
        
        if (shouldCompareSpec)
        {
            installationTasks.Add(this._oasdiffManager.InstallOasdiffAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
    }
}