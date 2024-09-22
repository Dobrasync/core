using LamashareCore.Models;

namespace LamashareCore;

public class PathUtil
{
    public static string SystemPathToLibraryPath(string systemPath, Library library)
    {
        return Path.GetRelativePath(library.LocalLibraryPath, systemPath);
    }
}