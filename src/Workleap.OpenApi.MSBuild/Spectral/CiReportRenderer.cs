namespace Workleap.OpenApi.MSBuild.Spectral;

internal static class CiReportRenderer
{
    public static void AttachReportToBuildAsync(string reportPath)
    {
        // Check if we are in ADO context and attach the report to the build summary
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI")))
        {
            Console.WriteLine("##vso[task.addattachment type=Distributedtask.Core.Summary;name=Spectral results;]{0}", reportPath);    
        }
    }
}