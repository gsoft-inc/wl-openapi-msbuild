namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a GenerateContract approach it will:
///     1. Generate the OpenAPI specification files from the code
///     2. Depending of <see cref="GenerateContractMode"/> it will either will compare the generated OpenAPI specification files against the provided specifications or it will update the source-controlled specification files 
///     2. Validate the OpenAPI specification files base on spectral rules 
/// </summary>
internal class GenerateContractProcess
{
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly SpectralManager _spectralManager;
    private readonly SwaggerManager _swaggerManager;
    private readonly SpecGeneratorManager _specGeneratorManager;
    private readonly OasdiffManager _oasdiffManager;

    internal GenerateContractProcess(ILoggerWrapper loggerWrapper, SpectralManager spectralManager, SwaggerManager swaggerManager, SpecGeneratorManager specGeneratorManager, OasdiffManager oasdiffManager)
    {
        this._loggerWrapper = loggerWrapper;
        this._spectralManager = spectralManager;
        this._swaggerManager = swaggerManager;
        this._specGeneratorManager = specGeneratorManager;
        this._oasdiffManager = oasdiffManager;
    }
    
    internal enum GenerateContractMode
    {
        SpecGeneration,
        SpecComparison,
    }
    
    internal async Task Execute(
        string[] openApiSpecificationFilesPath,
        string[] openApiSwaggerDocumentNames,
        string openApiSpectralRulesetUrl,
        GenerateContractMode mode,
        CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Installing dependencies...");
        await this.InstallDependencies(mode, cancellationToken);
        
        this._loggerWrapper.LogMessage("Running Swagger...");
        var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();

        if (mode == GenerateContractMode.SpecGeneration)
        {
            this._loggerWrapper.LogMessage("Generating specification files...");
            await this._specGeneratorManager.UpdateSpecificationFilesAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        } 
        else
        {
            this._loggerWrapper.LogMessage("Running Oasdiff...");
            await this._oasdiffManager.RunOasdiffAsync(openApiSpecificationFilesPath, generateOpenApiDocsPath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Running Spectral...");
        await this._spectralManager.RunSpectralAsync(openApiSpecificationFilesPath, openApiSpectralRulesetUrl, cancellationToken);
    }

    private async Task InstallDependencies(
        GenerateContractMode mode,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        installationTasks.Add(this._spectralManager.InstallSpectralAsync(cancellationToken));        
        installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
        
        if (mode == GenerateContractMode.SpecComparison)
        {
            installationTasks.Add(this._oasdiffManager.InstallOasdiffAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
    }
}