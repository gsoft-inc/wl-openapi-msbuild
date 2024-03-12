namespace Workleap.OpenApi.MSBuild;

internal sealed class HttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpClient _httpClient = SharedHttpClient.Instance;

    public async Task DownloadFileToDestinationAsync(string url, string destination, CancellationToken cancellationToken)
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
                    await SaveFileFromResponseAsync(destination, retryResponseStream, cancellationToken);
                }
                else
                {
                    throw new OpenApiTaskFailedException($"{url} could not be downloaded.");
                }
            }
            else
            {
                await SaveFileFromResponseAsync(destination, responseStream, cancellationToken);
            }
        }
        catch (Exception)
        {
            File.Delete(destination);
            throw;
        }
    }

    private static async Task SaveFileFromResponseAsync(string destination, Stream responseStream, CancellationToken cancellationToken)
    {
        using var fileTarget = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

        // In order to use cancellationToken we need to specify a bufferSize, so we just use the default value
        await responseStream.CopyToAsync(fileTarget, 81920, cancellationToken);
    }
}