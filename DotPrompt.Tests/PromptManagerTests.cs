namespace DotPrompt.Tests;

public class PromptManagerTests
{
    [Fact]
    public void PromptManager_CreatedWithDefaultConstructor_LoadsPromptsFromDefaultPath()
    {
        var manager = new PromptManager();

        var expectedPrompts = new List<string> { "basic", "example-with-name" };
        var actualPrompts = manager.ListPromptFileNames().ToList();
        
        Assert.Equal(expectedPrompts.Count, actualPrompts.Count);
        Assert.Contains("basic", actualPrompts);
        Assert.Contains("example-with-name", actualPrompts);
    }

    [Fact]
    public void PromptManager_WithFilePromptStoreSpecified_LoadsPromptsFromSpecifiedLocation()
    {
        var promptStore = new FilePromptStore("manager-prompts");
        var manager = new PromptManager(promptStore);

        var expectedPrompts = new List<string> { "basic", "example-with-name" };
        var actualPrompts = manager.ListPromptFileNames().ToList();
        
        Assert.Equal(expectedPrompts.Count, actualPrompts.Count);
        Assert.Contains("basic", actualPrompts);
        Assert.Contains("example-with-name", actualPrompts);
    }

    [Fact]
    public void PromptManager_WithPathSpecified_LoadsPromptsFromSpecifiedLocation()
    {
        var manager = new PromptManager("manager-prompts");

        var expectedPrompts = new List<string> { "basic", "example-with-name" };
        var actualPrompts = manager.ListPromptFileNames().ToList();
        
        Assert.Equal(expectedPrompts.Count, actualPrompts.Count);
        Assert.Contains("basic", actualPrompts);
        Assert.Contains("example-with-name", actualPrompts);
    }
    
    [Fact]
    public void PromptManager_ListPromptFileNamesWithVersions_ReturnsListOfPromptFileNamesAndVersions()
    {
        var manager = new PromptManager();
        
        var expectedPrompts = new List<string> { "basic:1", "example-with-name:1" };
        var actualPrompts = manager.ListPromptFileNamesWithVersions().ToList();
        
        Assert.Equal(expectedPrompts.Count, actualPrompts.Count);
        foreach (var expectedPrompt in expectedPrompts)
        {
            Assert.Contains(expectedPrompt, actualPrompts);
        }
    }

    [Fact]
    public void PromptManager_WithDuplicateNames_ThrowsException()
    {
        var promptStore = new FilePromptStore("duplicate-name-prompts");
        var act = () => new PromptManager(promptStore);

        var exception = Assert.Throws<DotPromptException>(act);

        Assert.Contains("a duplicate exists", exception.Message);
    }

    [Fact]
    public void PromptManager_WithDifferentVersions_LoadsSuccessfully()
    {
        var manager = new PromptManager("multiple-version-prompts");
        
        var expectedPrompts = new List<string> { "basic:1", "basic:2" };
        var actualPrompts = manager.ListPromptFileNamesWithVersions().ToList();
        
        Assert.Equal(expectedPrompts.Count, actualPrompts.Count);
        foreach (var expectedPrompt in expectedPrompts)
        {
            Assert.Contains(expectedPrompt, actualPrompts);
        }
    }

    [Fact]
    public void PromptManager_WhenRequestedByName_ReturnsLatestVersion()
    {
        var manager = new PromptManager("multiple-version-prompts");
        
        var expectedPrompt = PromptFile.FromFile("multiple-version-prompts/basic-new.prompt");
        var actualPrompt = manager.GetPromptFile("basic");
        
        Assert.Equivalent(expectedPrompt, actualPrompt, strict: true);
    }

    [Fact]
    public void PromptManager_WhenRequestedByNameAndVersion_ReturnsCorrectVersion()
    {
        var manager = new PromptManager("multiple-version-prompts");
        
        var expectedPrompt = PromptFile.FromFile("multiple-version-prompts/basic.prompt");
        var actualPrompt = manager.GetPromptFile("basic", 1);
        
        Assert.Equivalent(expectedPrompt, actualPrompt, strict: true);
    }
    
    [Fact]
    public void PromptManager_WhenRequestedByNameAndInvalidVersion_ThrowsException()
    {
        var manager = new PromptManager("multiple-version-prompts");
        
        var act = () => manager.GetPromptFile("basic", 3);
        
        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Equal("No prompt file with that name and version has been loaded", exception.Message);   
    }

    [Fact]
    public void GetPromptFile_WhenRequestedWithValidName_LoadsExpectedPromptFile()
    {
        var manager = new PromptManager();

        var expectedPromptFile = PromptFile.FromFile("prompts/BASIC.PROMPT");
        
        Assert.Equivalent(manager.GetPromptFile("basic"), expectedPromptFile, strict: true);
    }

    [Fact]
    public void GetPromptFile_WhenRequestedWithInvalidName_ThrowsException()
    {
        var manager = new PromptManager();

        var act = () => manager.GetPromptFile("not-this");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Equal("No prompt file with that name has been loaded", exception.Message);
    }
}