namespace Workleap.OpenApi.MSBuild;

public class OpenApiTaskFailedException : Exception
{
    public OpenApiTaskFailedException(string message) : base(message)
    {
    }
}