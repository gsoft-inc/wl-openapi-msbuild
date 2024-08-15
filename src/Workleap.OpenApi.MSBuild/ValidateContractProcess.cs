using Microsoft.Build.Framework;
using Workleap.OpenApi.MSBuild.Spectral;

namespace Workleap.OpenApi.MSBuild;

/// <summary>
/// For a ValidateContract approach it will:
///     1. Validate the OpenAPI specification files base on spectral rules
///     2. If <see cref="CompareCodeAgainstSpecFile"/> is enabled, will generate the OpenAPI specification files from the code and validate if it match the provided specifications.
/// </summary>
internal class ValidateContractProcess
{
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly SpectralInstaller _spectralInstaller;
    private readonly SpectralRulesetManager _spectralRulesetManager;
    private readonly SpectralRunner _spectralRunner;
    private readonly SwaggerManager _swaggerManager;
    private readonly OasdiffManager _oasdiffManager;

    internal ValidateContractProcess(
        ILoggerWrapper loggerWrapper,
        SpectralInstaller spectralInstaller,
        SpectralRulesetManager spectralRulesetManager,
        SpectralRunner spectralRunner,
        SwaggerManager swaggerManager,
        OasdiffManager oasdiffManager)
    {
        this._loggerWrapper = loggerWrapper;
        this._spectralInstaller = spectralInstaller;
        this._spectralRulesetManager = spectralRulesetManager;
        this._spectralRunner = spectralRunner;
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
        CompareCodeAgainstSpecFile compareCodeAgainstSpecFile,
        CancellationToken cancellationToken)
    {
        if (!this.CheckIfBaseSpecExists(openApiSpecificationFiles, openApiToolsDirectoryPath))
        {
            return false;
        }

        this._loggerWrapper.LogMessage("Installing dependencies...  ");
        var dependenciesResult = await this.InstallDependencies(compareCodeAgainstSpecFile, cancellationToken);

        if (compareCodeAgainstSpecFile == CompareCodeAgainstSpecFile.Enabled)
        {
            this._loggerWrapper.LogMessage("Running Swagger...");
            var generateOpenApiDocsPath = (await this._swaggerManager.RunSwaggerAsync(openApiSwaggerDocumentNames, cancellationToken)).ToList();

            this._loggerWrapper.LogMessage("Running Oasdiff...");
            await this._oasdiffManager.RunOasdiffAsync(openApiSpecificationFiles, generateOpenApiDocsPath, cancellationToken);
        }

        this._loggerWrapper.LogMessage("Running Spectral...");
        await this._spectralRunner.RunSpectralAsync(openApiSpecificationFiles, dependenciesResult.SpectralExecutablePath, dependenciesResult.SpectralRulesetPath, cancellationToken);

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

    private async Task<DependenciesResult> InstallDependencies(
        CompareCodeAgainstSpecFile compareCodeAgainstSpecFile,
        CancellationToken cancellationToken)
    {
        var installationTasks = new List<Task>();

        var spectralRulesetTask = this._spectralRulesetManager.GetLocalSpectralRulesetFile(cancellationToken);
        installationTasks.Add(spectralRulesetTask);

        var spectralInstallerTask = this._spectralInstaller.InstallSpectralAsync(cancellationToken);
        installationTasks.Add(spectralInstallerTask);

        if (compareCodeAgainstSpecFile == CompareCodeAgainstSpecFile.Enabled)
        {
            installationTasks.Add(this._swaggerManager.InstallSwaggerCliAsync(cancellationToken));
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