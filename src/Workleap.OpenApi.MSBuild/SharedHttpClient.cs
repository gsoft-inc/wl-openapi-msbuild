// Inspired from: https://www.meziantou.net/resilient-httpclient-with-or-without-polly.htm

using System.Net;

namespace Workleap.OpenApi.MSBuild;

internal static class SharedHttpClient
{
    public static HttpClient Instance { get; } = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient(new HttpRetryMessageHandler(new HttpClientHandler()), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
    }

    private sealed class HttpRetryMessageHandler : DelegatingHandler
    {
        public HttpRetryMessageHandler(HttpMessageHandler handler)
            : base(handler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var defaultDelay = TimeSpan.FromMilliseconds(200);
            for (var i = 1; ; i++, defaultDelay += defaultDelay)
            {
                TimeSpan? delayHint = null;
                HttpResponseMessage? result = null;

                try
                {
                    result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (!IsLastAttempt(i) && ((int)result.StatusCode >= 500 || result.StatusCode is HttpStatusCode.RequestTimeout or (HttpStatusCode)429))
                    {
                        // Use "Retry-After" value, if available. Typically, this is sent with
                        // either a 503 (Service Unavailable) or 429 (Too Many Requests):
                        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After
                        delayHint = result.Headers.RetryAfter switch
                        {
                            { Date: { } date } => date - DateTimeOffset.UtcNow,
                            { Delta: { } delta } => delta,
                            _ => null,
                        };

                        result.Dispose();
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (HttpRequestException)
                {
                    result?.Dispose();
                    if (IsLastAttempt(i))
                    {
                        throw;
                    }
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
                {
                    // catch "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing"
                    result?.Dispose();
                    if (IsLastAttempt(i))
                    {
                        throw;
                    }
                }

                await Task.Delay(delayHint is { } someDelay && someDelay > TimeSpan.Zero ? someDelay : defaultDelay, cancellationToken).ConfigureAwait(false);

                static bool IsLastAttempt(int i) => i >= maxRetries;
            }
        }
    }
}