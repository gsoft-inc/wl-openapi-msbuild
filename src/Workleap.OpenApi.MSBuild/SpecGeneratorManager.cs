using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

internal class SpecGeneratorManager
{
    private readonly ILoggerWrapper _loggerWrapper;

    public SpecGeneratorManager(ILoggerWrapper loggerWrapper)
    {
        this._loggerWrapper = loggerWrapper;
    }

    /// <summary>
    /// Will overwrite the sourced-control specification files with the generated ones.
    /// </summary>
    public async Task UpdateSpecificationFilesAsync(IReadOnlyCollection<string> sourcedControlOpenApiSpecFiles, IReadOnlyCollection<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("\n ******** Specification Generator: Updating specification files. ******** \n", MessageImportance.High);
        
        var filesPath = generatedOpenApiSpecFiles.ToDictionary(Path.GetFileName, x => x);
        
        foreach (var baseSpecFile in sourcedControlOpenApiSpecFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var fileName = Path.GetFileName(baseSpecFile);
            
            var isFileFound = filesPath.TryGetValue(fileName, out var generatedSpecFilePath);
            if (!isFileFound || string.IsNullOrEmpty(generatedSpecFilePath))
            {
                this._loggerWrapper.LogWarning("Could not find a generated spec file for {0}.", fileName);
                continue;
            }
            
            this._loggerWrapper.LogMessage("=> Overwriting {0} with {1}.", MessageImportance.High, fileName, generatedSpecFilePath);
            File.Copy(generatedSpecFilePath, baseSpecFile, true);
        }
        
        this._loggerWrapper.LogMessage("\n **************************************************************** \n", MessageImportance.High);
    }
}