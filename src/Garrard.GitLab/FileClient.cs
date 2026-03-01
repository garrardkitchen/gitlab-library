using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for local file system operations. No GitLab authentication required.
/// </summary>
public sealed class FileClient
{
    public void RemoveTempFolder(string clonePath)
    {
        if (Directory.Exists(clonePath))
            Directory.Delete(clonePath, true);
    }

    public void CopyFiles(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(destinationPath))
            Directory.CreateDirectory(destinationPath);

        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            if (dirPath.EndsWith(".git") || dirPath.Contains(".git/"))
                continue;
            Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
        }

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            if (newPath.EndsWith(".git") || newPath.Contains(".git/"))
                continue;
            File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }
    }

    public void CreateFileWithContent(string folderPath, string fileName, string content)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Replaces a placeholder in a file with a specific value asynchronously.
    /// </summary>
    public async Task<Result> ReplacePlaceholderInFile(
        string filePath,
        string placeholderKey,
        string placeholderValue,
        string newValue,
        string? assignmentOperator = ":",
        Action<string>? logAction = null)
    {
        if (!File.Exists(filePath))
            return Result.Failure($"File not found: {filePath}");

        try
        {
            string content = await File.ReadAllTextAsync(filePath);
            string pattern = $"{Regex.Escape(placeholderKey)}{assignmentOperator} {Regex.Escape(placeholderValue)}";
            string replacement = $"{placeholderKey}{assignmentOperator} {newValue}";

            bool containsPattern = Regex.IsMatch(content, pattern);
            if (!containsPattern)
            {
                string message = $"Pattern '{placeholderKey}{assignmentOperator} {placeholderValue}' not found in file {filePath}";
                return Result.Failure(message);
            }

            logAction?.Invoke($"Pattern '{placeholderKey}{assignmentOperator} {placeholderValue}' found in file {filePath}");

            string newContent = Regex.Replace(content, pattern, replacement);
            await File.WriteAllTextAsync(filePath, newContent);

            string successMessage = $"Successfully replaced '{placeholderValue}' with '{newValue}' in file {filePath}";
            logAction?.Invoke(successMessage);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error replacing placeholder in file {filePath}: {ex.Message}");
        }
    }
}
