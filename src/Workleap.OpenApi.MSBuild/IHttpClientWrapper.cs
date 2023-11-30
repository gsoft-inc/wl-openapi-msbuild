namespace Workleap.OpenApi.MSBuild;

internal interface IHttpClientWrapper
{
    Task DownloadFileToDestinationAsync(string url, string destination);
}