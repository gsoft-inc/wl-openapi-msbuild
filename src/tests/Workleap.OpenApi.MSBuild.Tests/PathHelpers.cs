#pragma warning disable CA1861
using Meziantou.Framework;

namespace Workleap.OpenApi.MSBuild.Tests;

internal static class PathHelpers
{
    public static FullPath GetGitRoot()
    {
        if (FullPath.CurrentDirectory().TryFindFirstAncestorOrSelf(current => Directory.Exists(current / ".git"), out var root))
        {
            return root;
        }

        throw new InvalidOperationException("root folder not found");
    }
}
