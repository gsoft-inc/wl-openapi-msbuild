namespace Workleap.OpenApi.MSBuild;

internal sealed class HttpClientWrapper : IHttpClientWrapper, IDisposable
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task DownloadFileToDestinationAsync(string url, string destination)
    {
        if (File.Exists(destination))
        {
            return;
        }

        try
        {
            using var responseStream = await this._httpClient.GetStreamAsync(url);

            if (responseStream == null)
            {
                using var retryResponseStream = await this._httpClient.GetStreamAsync(url);
                if (retryResponseStream != null)
                {
                    await SaveFileFromResponseAsync(destination, retryResponseStream);
                }
                else
                {
                    throw new OpenApiTaskFailedException($"{url} could not be downloaded.");
                }
            }
            else
            {
                await SaveFileFromResponseAsync(destination, responseStream);
            }
        }
        catch (Exception)
        {
            File.Delete(destination);
            throw;
        }
    }

    private static async Task SaveFileFromResponseAsync(string destination, Stream responseStream)
    {
        using var fileTarget = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

        await responseStream.CopyToAsync(fileTarget);
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
    }
}