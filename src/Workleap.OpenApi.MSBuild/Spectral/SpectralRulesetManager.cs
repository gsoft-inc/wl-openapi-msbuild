using Microsoft.Build.Framework;
using YamlDotNet.RepresentationModel;

namespace Workleap.OpenApi.MSBuild.Spectral;

internal sealed class SpectralRulesetManager
{
    private const string SpectralVersion = "0.8.0";
    private const string SpectralDownloadUrlFormat = "https://raw.githubusercontent.com/gsoft-inc/wl-api-guidelines/{0}/.spectral.{1}.yaml";
    
    private readonly ILoggerWrapper _loggerWrapper;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string spectralProfile;
    private readonly string _spectralRulesetPath;

    public SpectralRulesetManager(
        ILoggerWrapper loggerWrapper, 
        IHttpClientWrapper httpClientWrapper,
        string spectralProfile,
        string? spectralFilePathInput)
    {
        this._loggerWrapper = loggerWrapper;
        this._httpClientWrapper = httpClientWrapper;

        this.spectralProfile = string.Format(SpectralDownloadUrlFormat, SpectralVersion, spectralProfile);

        if (spectralFilePathInput != null && !string.IsNullOrEmpty(spectralFilePathInput))
        {
            this._spectralRulesetPath = spectralFilePathInput;
        }
        else
        {
            this._spectralRulesetPath = GetProfileRulesetUrl(this.spectralProfile);
        }
    }

    private static string GetProfileRulesetUrl(string spectralProfile)
    {
        return string.Format(SpectralDownloadUrlFormat, SpectralVersion, spectralProfile);
    }

    public async Task<string> GetSpectralRulesetFile(CancellationToken cancellationToken)
    {
        // For remote ruleset we download the file for optimization and reduce spectral flakyness
        if (!IsLocalFile(this._spectralRulesetPath))
        {
            this._loggerWrapper.LogMessage("Downloading ruleset.");
            var downloadedFilePath = await this.DownloadFileAsync(this._spectralRulesetPath, cancellationToken);

            return downloadedFilePath;
        }
        
        // For local custom rules if they are not extendings any rules we extend them with Workleap rules
        if (IsLocalFile(this._spectralRulesetPath) && !IsRulesetHaveExtendsProperty(this._spectralRulesetPath))
        {
            this._loggerWrapper.LogMessage("Extending ruleset with Workleap rules.");
            var copiedFilePath = await CopyAndExtendRuleset(this._spectralRulesetPath, GetProfileRulesetUrl(this.spectralProfile), cancellationToken);
            return copiedFilePath;
        }

        return this._spectralRulesetPath;
    }
    
    private static bool IsRulesetHaveExtendsProperty(string customSpectralFilePath)
    {
        using var reader = new StreamReader(customSpectralFilePath);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var hasExtendsProperty = yamlStream.Documents.FirstOrDefault()?.RootNode is YamlMappingNode root && 
               root.Children.ContainsKey(new YamlScalarNode("extends"));

        return hasExtendsProperty;
    }
    
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