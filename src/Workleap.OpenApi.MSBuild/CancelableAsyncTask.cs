using Microsoft.Build.Framework;

namespace Workleap.OpenApi.MSBuild;

// Cancellation and disposable support inspired by:
// https://github.com/NuGet/NuGet.Client/blob/6.9.0.18/src/NuGet.Core/NuGet.Build.Tasks/RestoreTask.cs
public abstract class CancelableAsyncTask : Microsoft.Build.Utilities.Task, ICancelableTask, IDisposable
{
    private readonly CancellationTokenSource _userCts = new CancellationTokenSource();
    private bool _disposed;

    public override bool Execute()
    {
        // Explicitly set the cancellation token timeout in case the task implementation hangs.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(this._userCts.Token);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        try
        {
            // MSBuild isn't asynchronous, so we need to block (https://github.com/dotnet/msbuild/issues/4174#issuecomment-463736428)
            return this.ExecuteAsync(timeoutCts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException ocex)
        {
            if (this._userCts.IsCancellationRequested)
            {
                // The build was canceled by the user, silently exit.
                return false;
            }

            // TODO print a message to the user that the task timed out?
            return false;
        }
        finally
        {
            // MSBuild does not seem to automatically call IDisposable.Dispose on task classes.
            this.Dispose();
        }
    }

    protected abstract Task<bool> ExecuteAsync(CancellationToken cancellationToken);

    public void Cancel()
    {
        this._userCts.Cancel();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            this._userCts.Dispose();
        }

        this._disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}