using System.IO;

namespace Garrard.GitLab;

public class FileOperations
{
    public static void RemoveTempFolder(string clonePath)
    {
        if (Directory.Exists(clonePath))
        {
            Directory.Delete(clonePath, true);
        }
    }

    public static void CopyFiles(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            if (dirPath.EndsWith(".git") || dirPath.Contains(".git/"))
            {
                continue;
            }
            Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
        }
        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            if (newPath.EndsWith(".git") || newPath.Contains(".git/"))
            {
                continue;
            }
            File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }
    }
}
