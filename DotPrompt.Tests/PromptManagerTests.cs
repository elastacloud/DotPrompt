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
    public void PromptManager_WithPathSpecified_LoadsPromptsFromSpecifiedLocation()
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
    public void PromptManager_WithDuplicateNames_ThrowsException()
    {
        var promptStore = new FilePromptStore("duplicate-name-prompts");
        var act = () => new PromptManager(promptStore);

        var exception = Assert.Throws<DotPromptException>(act);

        Assert.Contains("a duplicate exists", exception.Message);
    }

    [Fact]
    public void GetPromptFile_WhenRequestedWithValidName_LoadsExpectedPromptFile()
    {
        var manager = new PromptManager();

        var expectedPromptFile = PromptFile.FromFile("prompts/basic.prompt");
        
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