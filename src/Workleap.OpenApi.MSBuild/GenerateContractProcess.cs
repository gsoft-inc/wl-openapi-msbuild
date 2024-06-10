using Microsoft.Build.Framework;
using Workleap.OpenApi.MSBuild.Spectral;

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
    private readonly SpectralInstaller _spectralInstaller;
    private readonly SpectralRulesetManager _spectralRulesetManager;
    private readonly SpectralRunner _spectralRunner;
    private readonly SwaggerManager _swaggerManager;
    private readonly SpecGeneratorManager _specGeneratorManager;
    private readonly OasdiffManager _oasdiffManager;

    internal GenerateContractProcess(
        ILoggerWrapper loggerWrapper, 
        SpectralInstaller spectralInstaller,
        SpectralRulesetManager spectralRulesetManager,
        SpectralRunner spectralRunner, 
        SwaggerManager swaggerManager, 
        SpecGeneratorManager specGeneratorManager, 
        OasdiffManager oasdiffManager)
    {
        this._loggerWrapper = loggerWrapper;
        this._spectralInstaller = spectralInstaller;
        this._spectralRulesetManager = spectralRulesetManager;
        this._spectralRunner = spectralRunner;
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
        GenerateContractMode mode,
        CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Installing dependencies...");
        var dependenciesResult = await this.InstallDependencies(mode, cancellationToken);
        
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
        await this._spectralRunner.RunSpectralAsync(generateOpenApiDocsPath, dependenciesResult.SpectralExecutablePath, dependenciesResult.SpectralRulesetPath, cancellationToken);
    }

    private async Task<DependenciesResult> InstallDependencies(
        GenerateContractMode mode,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();    
        
        var spectralRulesetTask = this._spectralRulesetManager.GetSpectralRulesetFile(cancellationToken);
        installationTasks.Add(spectralRulesetTask);        

        var spectralInstallerTask = this._spectralInstaller.InstallSpectralAsync(cancellationToken);
        installationTasks.Add(spectralInstallerTask);
        
        installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
        
        if (mode == GenerateContractMode.SpecComparison)
        {
            installationTasks.Add(this._oasdiffManager.InstallOasdiffAsync(cancellationToken));
        }

        await Task.WhenAll(installationTasks);
        this._loggerWrapper.LogMessage("Finished installing OpenAPI dependencies.", MessageImportance.High);

        var spectralRulesetPath = await spectralRulesetTask;
        var spectralExecutablePath = await spectralInstallerTask;
        
        return new DependenciesResult(spectralRulesetPath, spectralExecutablePath);
    }
    
    private class DependenciesResult
    {
        public DependenciesResult(string spectralRulesetPath, string spectralExecutablePath)
        {
            this.SpectralRulesetPath = spectralRulesetPath;
            this.SpectralExecutablePath = spectralExecutablePath;
        }

        public string SpectralRulesetPath { get; }
        
        public string SpectralExecutablePath { get; }
    }
}