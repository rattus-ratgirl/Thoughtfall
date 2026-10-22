namespace Thoughfall.Application;

/// <summary>
/// Provides methods for resolving and managing directory paths.
/// </summary>
internal static class DataPaths
{
    /// <summary>
    /// Resolve directory aliases before comparing destinations or deriving instance identity.
    /// </summary>
    /// <param name="path">The directory path to resolve.</param>
    /// <returns>The resolved directory path.</returns>
    internal static string ResolveDirectory(string path)
    {
        return ResolveDirectory(
            Path.GetFullPath(path),
            new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
        );
    }

    private static string ResolveDirectory(string path, HashSet<string> links)
    {
        while (true)
        {
            var directory = new DirectoryInfo(path);

            if (directory.Parent is null)
                return directory.FullName;

            var parent = ResolveDirectory(directory.Parent.FullName, links);
            directory = new DirectoryInfo(Path.Combine(parent, directory.Name));

            if (directory.LinkTarget is null)
                return directory.FullName;

            if (!links.Add(directory.FullName))
                throw new IOException("The directory contains a circular link.");

            var target = directory.ResolveLinkTarget(true) ?? throw new IOException("The directory link cannot be resolved.");
            path = target.FullName;
        }
    }
}
