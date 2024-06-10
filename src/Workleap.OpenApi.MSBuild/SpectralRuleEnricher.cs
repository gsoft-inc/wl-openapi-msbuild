using Microsoft.Build.Framework;
using YamlDotNet.RepresentationModel;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralRuleEnricher
{
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;

    public SpectralRuleEnricher(ILoggerWrapper loggerWrapper, IHttpClientWrapper httpClientWrapper)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;
    }

    public async Task<string> GetSpectralFile(string initialpath, CancellationToken cancellationToken)
    {
        var updatedPath = initialpath;
        
        if (!IsLocalFile(initialpath))
        {
            // We download the file before to isolate flakiness in the spectral execution
            this._loggerWrapper.LogMessage("Downloading ruleset.");
            updatedPath = await this.DownloadFileAsync(initialpath, cancellationToken);
        }
        else if (!IsRulesetHaveExtendedsProperty(initialpath))
        {
            // We download the file before to isolate flakiness in the spectral execution
            this._loggerWrapper.LogMessage("Extending ruleset with Workleap rules.");
            updatedPath = await CopyAndExtendRuleset(initialpath, "https://raw.githubusercontent.com/gsoft-inc/wl-api-guidelines/main/.spectral.frontend.yaml", cancellationToken);
        }

        return updatedPath;
    }
    
    private static bool IsRulesetHaveExtendedsProperty(string customSpectralFilePath)
    {
        using var reader = new StreamReader(customSpectralFilePath);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var hasExtendsProperty = yamlStream.Documents.FirstOrDefault()?.RootNode is YamlMappingNode root && 
               root.Children.ContainsKey(new YamlScalarNode("extends"));

        return hasExtendsProperty;
    }
    
    /// <summary>
    /// Extends ruleset with spectral profile
    /// </summary>
    /// <returns>Updated ruleset path</returns>
    private static async Task<string> CopyAndExtendRuleset(string initialPath, string extendsUrl, CancellationToken cancellationToken)
    {
        var outputFilePath = Path.ChangeExtension(Path.GetTempFileName(), "yaml");
        
        using var sourceReader = new StreamReader(initialPath);
        using var destinationWriter = new StreamWriter(outputFilePath);

        await destinationWriter.WriteLineAsync($"extends: [{extendsUrl}]");

        while (await sourceReader.ReadLineAsync() is { } line)
        {
            await destinationWriter.WriteLineAsync(line);
        }

        return outputFilePath;
    }
    
    private static bool IsLocalFile(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.IsFile;
        }
        
        return true;
    }
    
    private async Task<string> DownloadFileAsync(string rulesetUrl, CancellationToken cancellationToken)
    {
        try
        {
            this._loggerWrapper.LogMessage("Downloading rule file {0}", MessageImportance.Normal, rulesetUrl);
            
            var outputFilePath = Path.ChangeExtension(Path.GetTempFileName(), "yaml");
            await this._httpClientWrapper.DownloadFileToDestinationAsync(rulesetUrl, outputFilePath, cancellationToken);
            
            this._loggerWrapper.LogMessage("Download completed", MessageImportance.Normal);

            return outputFilePath;
        }
        catch (Exception e)
        {
            this._loggerWrapper.LogWarning(e.Message);
            throw new OpenApiTaskFailedException($"Failed to download {rulesetUrl}.");
        }
    }

}