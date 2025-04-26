namespace DotPrompt.Tests;

public class FilePromptStoreTests
{
    [Fact]
    public void FilePromptStore_WithNoParameters_CreatesInstanceUsingDefaultPath()
    {
        var promptStore = new FilePromptStore();
        
        Assert.Equal("prompts", promptStore.PromptDirectory.Name);
    }

    [Fact]
    public void FilePromptStore_WithValidPath_CreatesInstance()
    {
        var promptStore = new FilePromptStore("manager-prompts");
        
        Assert.Equal("manager-prompts", promptStore.PromptDirectory.Name);
    }
    
    [Fact]
    public void FilePromptStore_WithInvalidPathSpecified_ThrowsException()
    {
        var act = () => new FilePromptStore("does-not-exist");

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("The specified path does not exist", exception.Message);
    }

    [Fact]
    public void Load_WithVaryingCaseNames_RetrievesPromptsFromDirectory()
    {
        var promptStore = new FilePromptStore();

        var promptFiles = promptStore.Load().ToList();

        Assert.Contains("basic", promptFiles.Select(p => p.Name));
        Assert.Contains("example-with-name", promptFiles.Select(p => p.Name));
    }

    [Fact]
    public void Load_WhenCalledOnDirectoryWithSubDirectories_RetrievesAllPromptFiles()
    {
        var promptStore = new FilePromptStore("manager-prompts");

        var promptFiles = promptStore.Load().ToList();

        Assert.Equal(2, promptFiles.Count);
    }

    [Fact]
    public void Save_WhenCalledWithValidPromptFile_SavesToFile()
    {
        using var tempDirectory = new TempDirectory();
        var originalPromptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");

        var promptStore = new FilePromptStore(tempDirectory.TempPath);
        
        var newFilePath = tempDirectory.GetTempFilePath();
        var newFileName = Path.GetFileName(newFilePath);
        promptStore.Save(originalPromptFile, newFileName);

        var savedPromptFile = PromptFile.FromFile(newFilePath);
        
        Assert.Equivalent(originalPromptFile, savedPromptFile, true);
    }

    [Fact]
    public void Save_WhenCalledWithMissingNameValue_ValueFromPromptIsUsed()
    {
        using var tempDirectory = new TempDirectory();
        var originalPromptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        originalPromptFile.Name = "missing-name-test";

        var promptStore = new FilePromptStore(tempDirectory.TempPath);
        promptStore.Save(originalPromptFile);

        var tempFiles = Directory.EnumerateFiles(tempDirectory.TempPath, "*.prompt")
            .Select(Path.GetFileName)
            .ToList();

        Assert.Contains("missing-name-test.prompt", tempFiles);
    }
    
    [Fact]
    public void Save_WhenCalledWithNoValidNameValue_ThrowsException()
    {
        using var tempDirectory = new TempDirectory();
        var originalPromptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        originalPromptFile.Name = string.Empty;
        
        var promptStore = new FilePromptStore(tempDirectory.TempPath);
        var act = () => promptStore.Save(originalPromptFile);
        
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("A name must be provided for the prompt file", exception.Message);
    }
}

public class TempDirectory : IDisposable
{
    private readonly DirectoryInfo _path = Directory.CreateTempSubdirectory("prompts_");

    public string TempPath => _path.FullName;

    public string GetTempFilePath()
    {
        return Path.Join(_path.FullName, $"{Path.GetRandomFileName()}.prompt");
    }

    public void Dispose()
    {
        if (_path.Exists)
        {
            _path.Delete(true);
        }
        
        GC.SuppressFinalize(this);
    }
}