using System.IO;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

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

    public static void CreateFileWithContent(string folderPath, string fileName, string content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Replaces a placeholder in a file with a specific value asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to be modified</param>
    /// <param name="placeholderKey">The key part before the placeholder (e.g. "TF_VAR_TFE_WORKSPACE_NAME: ")</param>
    /// <param name="placeholderValue">The placeholder value to be replaced (e.g. "<enter-workload-name>")</param>
    /// <param name="newValue">The new value to replace the placeholder with</param>
    /// <param name="assignmentOperator">The string to uses in the pattern matching for the assignment operator for example '=' or ':'</param>
    /// <param name="logAction">An optional action that accepts a string message for logging or notification purposes</param>
    /// <returns>A Task of Result indicating success or failure with an error message</returns>
    public static async Task<Result> ReplacePlaceholderInFile(string filePath, string placeholderKey, string placeholderValue, string newValue, string? assignmentOperator = ":",  Action<string> logAction = null)
    {
        if (!File.Exists(filePath))
        {
            string message = $"File not found: {filePath}";
            return Result.Failure(message);
        }

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
            string errorMessage = $"Error replacing placeholder in file {filePath}: {ex.Message}";
            return Result.Failure(errorMessage);
        }
    }
}
