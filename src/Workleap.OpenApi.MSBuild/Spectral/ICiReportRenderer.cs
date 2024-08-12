namespace Workleap.OpenApi.MSBuild.Spectral;

internal interface ICiReportRenderer
{
    public Task AttachReportToBuildAsync(string reportPath);
}