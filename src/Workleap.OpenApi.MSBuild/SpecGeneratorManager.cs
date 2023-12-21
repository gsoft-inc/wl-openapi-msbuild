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
    public async Task UpdateSpecificationFilesAsync(IEnumerable<string> sourcedControlOpenApiSpecFiles, IEnumerable<string> generatedOpenApiSpecFiles, CancellationToken cancellationToken)
    {
        this._loggerWrapper.LogMessage("Starting updating specification files.");
        
        foreach (var baseSpecFile in sourcedControlOpenApiSpecFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var fileName = Path.GetFileName(baseSpecFile);
            var generatedSpecFilePath = generatedOpenApiSpecFiles.FirstOrDefault(x => x.Contains(fileName));
            
            if (generatedSpecFilePath == null)
            {
                this._loggerWrapper.LogWarning($"Could not find a generated spec file for {baseSpecFile}.");
                continue;
            }
            
            this._loggerWrapper.LogMessage($"Overwriting {baseSpecFile} with {generatedSpecFilePath}.");
            File.Copy(generatedSpecFilePath, baseSpecFile, true);
        }
    }
}