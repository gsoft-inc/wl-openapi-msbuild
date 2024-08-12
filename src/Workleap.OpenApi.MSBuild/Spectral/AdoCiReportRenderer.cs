namespace Workleap.OpenApi.MSBuild.Spectral;

internal sealed class AdoCiReportRenderer : ICiReportRenderer
{
    public async Task AttachReportToBuildAsync(string reportPath)
    {
        // Attach the report to the build summary
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_NAME")))
        {
            Console.WriteLine("##vso[task.addattachment type=Distributedtask.Core.Summary;name=Spectral results;]{0}", reportPath);    
        }
    }
}