namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a Contract First approach it will:
///     1. Validate the OpenAPI specification files base on spectral rules
///     2. If <see cref="CompareCodeAgainstSpecFile"/> is enabled, will generate the OpenAPI specification files from the code and validate if it match the provided specifications.
/// </summary>
internal class ContractFirstProcess
{
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly SpectralManager _spectralManager;
    private readonly SwaggerManager _swaggerManager;
    private readonly OasdiffManager _oasdiffManager;
    
    internal ContractFirstProcess(ILoggerWrapper loggerWrapper, SpectralManager spectralManager, SwaggerManager swaggerManager, OasdiffManager oasdiffManager)
    {
        this._loggerWrapper = loggerWrapper;
        this._spectralManager = spectralManager;
        this._swaggerManager = swaggerManager;
        this._oasdiffManager = oasdiffManager;
    }

    internal enum CompareCodeAgainstSpecFile
    {
        Disabled,
        Enabled,
    }

    internal async Task<bool> Execute(
        string[] openApiSpecificationFiles,
        string openApiToolsDirectoryPath,
        string[] openApiSwaggerDocumentNames,
        string openApiSpectralRulesetUrl,
        CompareCodeAgainstSpecFile compareCodeAgainstSpecFile,
        CancellationToken cancellationToken)
    {
        if (!this.CheckIfBaseSpecExists(openApiSpecificationFiles, openApiToolsDirectoryPath))
        {
            return false;
        }

        this._loggerWrapper.LogMessage("Installing dependencies...");
        await this.InstallDependencies(compareCodeAgainstSpecFile, cancellationToken);
        
        if (compareCodeAgainstSpecFile == CompareCodeAgainstSpecFile.Enabled)
        {
            this._loggerWrapper.LogMessage("Running Swagger...");
            var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();
            
            this._loggerWrapper.LogMessage("Running Oasdiff...");
            await this._oasdiffManager.RunOasdiffAsync(openApiSpecificationFiles, generateOpenApiDocsPath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Running Spectral...");
        await this._spectralManager.RunSpectralAsync(openApiSpecificationFiles, openApiSpectralRulesetUrl, cancellationToken);

        return true;
    }
    
    private bool CheckIfBaseSpecExists(
        string[] openApiSpecificationFiles, 
        string openApiToolsDirectoryPath)
    {
        foreach (var file in openApiSpecificationFiles)
        {
            if (File.Exists(file))
            {
                continue;
            }

            this._loggerWrapper.LogWarning(
                "The file '{0}' does not exist. If you are running this for the first time, we have generated specification here '{1}' which can be used as base specification. " +
                "Please copy specification file(s) to your project directory and rebuild.", 
                file, 
                openApiToolsDirectoryPath);

            return false;
        }

        return true;
    }
    
    private async Task InstallDependencies(
        CompareCodeAgainstSpecFile compareCodeAgainstSpecFile,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        installationTasks.Add(this._spectralManager.InstallSpectralAsync(cancellationToken));        
        
        if (compareCodeAgainstSpecFile == CompareCodeAgainstSpecFile.Enabled)
        {
            installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
            installationTasks.Add(this._oasdiffManager.InstallOasdiffAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
    }
}