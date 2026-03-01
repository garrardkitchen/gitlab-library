using System.ComponentModel;
using Garrard.GitLab;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="FileOperations"/>. No GitLab auth required.</summary>
[McpServerToolType]
public sealed class FileTools
{
    [McpServerTool(Name = "file_remove_temp_folder"), Description("Deletes a temporary folder and all its contents.")]
    public string RemoveTempFolder(
        [Description("The path of the folder to delete.")] string clonePath)
    {
        FileOperations.RemoveTempFolder(clonePath);
        return $"Folder '{clonePath}' removed (if it existed).";
    }

    [McpServerTool(Name = "file_copy_files"), Description("Copies all files from a source directory to a destination directory, skipping .git directories.")]
    public string CopyFiles(
        [Description("The source directory path.")] string sourcePath,
        [Description("The destination directory path.")] string destinationPath)
    {
        FileOperations.CopyFiles(sourcePath, destinationPath);
        return $"Files copied from '{sourcePath}' to '{destinationPath}'.";
    }

    [McpServerTool(Name = "file_create_with_content"), Description("Creates a new file with specified content inside a folder, creating the folder if it does not exist.")]
    public string CreateFileWithContent(
        [Description("The folder path where the file should be created.")] string folderPath,
        [Description("The name of the file to create.")] string fileName,
        [Description("The content to write into the file.")] string content)
    {
        FileOperations.CreateFileWithContent(folderPath, fileName, content);
        return $"File '{fileName}' created in '{folderPath}'.";
    }

    [McpServerTool(Name = "file_replace_placeholder"), Description("Replaces a placeholder value in a file with a new value using pattern matching.")]
    public async Task<string> ReplacePlaceholderInFile(
        [Description("The path to the file to modify.")] string filePath,
        [Description("The key part before the placeholder (e.g. 'TF_VAR_NAME').")] string placeholderKey,
        [Description("The placeholder value to replace (e.g. '<enter-value>').")] string placeholderValue,
        [Description("The new value to substitute in.")] string newValue,
        [Description("The assignment operator used in the file, e.g. ':' or '=' (default: ':').")] string assignmentOperator = ":")
    {
        var result = await FileOperations.ReplacePlaceholderInFile(filePath, placeholderKey, placeholderValue, newValue, assignmentOperator);
        return ToolHelper.Serialize(result);
    }
}
