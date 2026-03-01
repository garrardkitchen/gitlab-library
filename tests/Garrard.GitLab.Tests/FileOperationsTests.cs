using Garrard.GitLab.Library;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="FileClient"/>.
/// These tests use real file system I/O via temporary directories.
/// </summary>
public class FileOperationsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileClient _fileClient = new FileClient();

    public FileOperationsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void RemoveTempFolder_ExistingFolder_DeletesFolder()
    {
        var folderToDelete = Path.Combine(_tempDir, "to-delete");
        Directory.CreateDirectory(folderToDelete);

        _fileClient.RemoveTempFolder(folderToDelete);

        Assert.False(Directory.Exists(folderToDelete));
    }

    [Fact]
    public void RemoveTempFolder_NonExistentFolder_DoesNotThrow()
    {
        var nonExistent = Path.Combine(_tempDir, "does-not-exist");
        var exception = Record.Exception(() => _fileClient.RemoveTempFolder(nonExistent));
        Assert.Null(exception);
    }

    [Fact]
    public void CreateFileWithContent_CreatesFileWithExpectedContent()
    {
        var folderPath = Path.Combine(_tempDir, "output");
        const string fileName = "test.txt";
        const string content = "Hello, world!";

        _fileClient.CreateFileWithContent(folderPath, fileName, content);

        var filePath = Path.Combine(folderPath, fileName);
        Assert.True(File.Exists(filePath));
        Assert.Equal(content, File.ReadAllText(filePath));
    }

    [Fact]
    public void CreateFileWithContent_FolderDoesNotExist_CreatesFolderThenFile()
    {
        var folderPath = Path.Combine(_tempDir, "new-folder", "nested");

        _fileClient.CreateFileWithContent(folderPath, "readme.md", "# Title");

        Assert.True(Directory.Exists(folderPath));
        Assert.True(File.Exists(Path.Combine(folderPath, "readme.md")));
    }

    [Fact]
    public void CopyFiles_CopiesAllFilesExcludingGitFolder()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        var destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(sourceDir);
        var gitDir = Path.Combine(sourceDir, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "content");
        File.WriteAllText(Path.Combine(gitDir, "config"), "git config");

        _fileClient.CopyFiles(sourceDir, destDir);

        Assert.True(File.Exists(Path.Combine(destDir, "file.txt")));
        Assert.False(Directory.Exists(Path.Combine(destDir, ".git")));
    }

    [Fact]
    public async Task ReplacePlaceholderInFile_ReplacesExpectedPattern()
    {
        var filePath = Path.Combine(_tempDir, "test.yml");
        await File.WriteAllTextAsync(filePath, "KEY: <placeholder>");

        var result = await _fileClient.ReplacePlaceholderInFile(filePath, "KEY", "<placeholder>", "new-value");

        Assert.True(result.IsSuccess);
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("KEY: new-value", content);
        Assert.DoesNotContain("<placeholder>", content);
    }

    [Fact]
    public async Task ReplacePlaceholderInFile_FileNotFound_ReturnsFailure()
    {
        var result = await _fileClient.ReplacePlaceholderInFile(
            Path.Combine(_tempDir, "missing.yml"), "KEY", "<placeholder>", "value");

        Assert.True(result.IsFailure);
        Assert.Contains("File not found", result.Error);
    }

    [Fact]
    public async Task ReplacePlaceholderInFile_PatternNotFound_ReturnsFailure()
    {
        var filePath = Path.Combine(_tempDir, "no-pattern.yml");
        await File.WriteAllTextAsync(filePath, "OTHER_KEY: something");

        var result = await _fileClient.ReplacePlaceholderInFile(filePath, "KEY", "<placeholder>", "value");

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
    }

    [Theory]
    [InlineData("KEY: <original>", "KEY", "<original>", "replaced", "KEY: replaced")]
    [InlineData("VAR: <val>", "VAR", "<val>", "foo", "VAR: foo")]
    public async Task ReplacePlaceholderInFile_VariousAssignments(
        string fileContent, string key, string placeholder, string newVal, string expectedContent)
    {
        var filePath = Path.Combine(_tempDir, $"test-{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(filePath, fileContent);

        var result = await _fileClient.ReplacePlaceholderInFile(filePath, key, placeholder, newVal, ":");

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        var actual = await File.ReadAllTextAsync(filePath);
        Assert.Contains(expectedContent, actual);
    }
}
