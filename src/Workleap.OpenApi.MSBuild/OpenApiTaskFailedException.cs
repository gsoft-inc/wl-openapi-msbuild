namespace Workleap.OpenApi.MSBuild.Exceptions;

public class OpenApiTaskFailedException : Exception
{
    public OpenApiTaskFailedException(string message) : base(message)
    {
    }
}