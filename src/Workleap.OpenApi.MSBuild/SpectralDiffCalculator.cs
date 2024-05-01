using System.Security.Cryptography;

namespace Workleap.OpenApi.MSBuild;

internal sealed class SpectralDiffCalculator
{
    private const string ChecksumExtension = "spectral-checksum";
    private const string SpectralRulesetChecksumItemName = "spectral-ruleset-checksum";
    
    private readonly string _spectralOutputDirectoryPath;

    public SpectralDiffCalculator(string spectralChecksumOutputDirectoryPath)
    {
        this._spectralOutputDirectoryPath = spectralChecksumOutputDirectoryPath;
    }
    
    public bool HasRulesetChangedSinceLastExecution(string spectralRulset)
    {
        var preciousRulesetChecksum = this.GetItemChecksum(SpectralRulesetChecksumItemName);
        var currentRulesetChecksum = GetFileChecksum(spectralRulset);

        var hasRulesetChanged = !string.Equals(preciousRulesetChecksum, currentRulesetChecksum, StringComparison.InvariantCultureIgnoreCase); 

        return hasRulesetChanged;
    }
    
    public bool HasOpenApiDocumentChangedSinceLastExecution(IReadOnlyCollection<string> openApiDocumentPaths)
    {
        foreach (var filePath in openApiDocumentPaths)
        {
            var itemName = Path.GetFileNameWithoutExtension(filePath);
            var checksum = GetFileChecksum(filePath);

            var previousChecksums = this.GetItemChecksum(itemName);
            
            if (!string.Equals(previousChecksums, checksum, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetFileChecksum(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
    
    private string GetItemChecksum(string itemName)
    {        
        var checksumFilePath = this.GetItemChecksumPath(itemName);
        if (!File.Exists(checksumFilePath))
        {
            return string.Empty;
        }
        
        return File.ReadAllText(checksumFilePath);
    }
    
    private void SetItemChecksum(string itemName, string checksum)
    {
        var checksumFilePath = this.GetItemChecksumPath(itemName);
        File.WriteAllText(this.GetItemChecksumPath(itemName), checksum);
    }

    private string GetItemChecksumPath(string itemName)
    {
        return Path.Combine(this._spectralOutputDirectoryPath, $"{itemName}.{ChecksumExtension}");
    }
    
    public void SaveCurrentExecutionChecksum(string spectralRulesetPath, IReadOnlyCollection<string> openApiDocumentPaths)
    {
        if (Directory.Exists(this._spectralOutputDirectoryPath))
        {
            Directory.Delete(this._spectralOutputDirectoryPath, true);
        }

        Directory.CreateDirectory(this._spectralOutputDirectoryPath);
        
        this.SetItemChecksum(SpectralRulesetChecksumItemName, GetFileChecksum(spectralRulesetPath));
        
        foreach (var documentPath in openApiDocumentPaths)
        {
            var itemName = Path.GetFileNameWithoutExtension(documentPath);
            var checksum = GetFileChecksum(documentPath);
            this.SetItemChecksum(itemName, checksum);
        }
    }
}