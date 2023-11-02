using System.Net;

namespace Workleap.OpenApi.MSBuild;

internal sealed class HttpClientWrapper : IHttpClientWrapper, IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task DownloadFileToDestinationAsync(string url, string destination, CancellationToken cancellationToken)
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
                throw new OpenApiTaskFailedException("Spectral could not be downloaded.");
            }
        }
        else
        {
            await SaveFileFromResponseAsync(destination, responseStream, cancellationToken);
        }
    }

    private static async Task SaveFileFromResponseAsync(string destination, Stream responseStream, CancellationToken cancellationToken)
    {
        using var fileTarget = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

        await responseStream.CopyToAsync(fileTarget, 81920, cancellationToken);
    }

    public void Dispose()
    {
        this._httpClient.Dispose();
    }
}