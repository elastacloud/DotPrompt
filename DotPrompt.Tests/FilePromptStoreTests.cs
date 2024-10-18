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
}