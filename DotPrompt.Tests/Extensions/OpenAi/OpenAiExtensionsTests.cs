using DotPrompt.Extensions.OpenAi;
using OpenAI.Chat;

namespace DotPrompt.Tests.Extensions.OpenAi;

public class OpenAiExtensionsTests
{
    [Fact]
    public void ToOpenAiChatMessages_WhenProvidedWithBothPrompts_ProducesTwoMessages()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");

        var messages = promptFile.ToOpenAiChatMessages(new Dictionary<string, object>
        {
            { "country", "Antarctica" },
            { "style", "Pirate" }
        }).ToList();
        
        Assert.Equal(2, messages.Count);
        Assert.IsType<SystemChatMessage>(messages[0]);
        Assert.IsType<UserChatMessage>(messages[1]);

        Assert.StartsWith("You are a helpful AI assistant that enjoys making penguin related puns",
            messages[0].Content[0].Text);
        Assert.StartsWith("I am looking at going on holiday to Antarctica", messages[1].Content[0].Text);
    }

    [Fact]
    public void ToOpenAiChatMessages_WhenProvidedWithUserPromptOnly_ProducesSingleMessage()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/with-name.prompt");

        var messages = promptFile.ToOpenAiChatMessages(new Dictionary<string, object>
        {
            { "country", "Antarctica" },
            { "style", "Pirate" }
        }).ToList();
        
        Assert.Single(messages);
        Assert.IsType<UserChatMessage>(messages[0]);

        Assert.StartsWith("I am looking at going on holiday to Antarctica", messages[0].Content[0].Text);
    }

    [Fact]
    public void ToOpenAiChatMessages_WhenProvidedWithFewShotPrompts_ProducesMessagesForEachPair()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic-fsp.prompt");

        var messages = promptFile.ToOpenAiChatMessages(new Dictionary<string, object>
        {
            { "topic", "improbability drive" }
        }).ToList();
        
        Assert.Equal(8, messages.Count);
        Assert.IsType<UserChatMessage>(messages[0]);
        Assert.IsType<AssistantChatMessage>(messages[1]);
        Assert.IsType<UserChatMessage>(messages[2]);
        Assert.IsType<AssistantChatMessage>(messages[3]);
        Assert.IsType<UserChatMessage>(messages[4]);
        Assert.IsType<AssistantChatMessage>(messages[5]);
        Assert.IsType<SystemChatMessage>(messages[6]);
        Assert.IsType<UserChatMessage>(messages[7]);

        Assert.Equal("How does machine learning differ from traditional programming?", messages[2].Content[0].Text);
        Assert.Equal(
            "Machine learning allows algorithms to learn from data and improve over time without being explicitly programmed.",
            messages[3].Content[0].Text);
        Assert.Equal(
            "Explain the impact of improbability drive on how we engage with technology as a society",
            messages[7].Content[0].Text);
    }

    [Fact]
    public void ToOpenAiChatCompletionOptions_WithAlLConfig_ReturnsAValidOptionsInstance()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        var options = promptFile.ToOpenAiChatCompletionOptions();
        
        Assert.NotNull(options.Temperature);
        Assert.Equal(0.9, options.Temperature.Value, 1e-2);
        Assert.NotNull(options.MaxOutputTokenCount);
        Assert.Equal(500, options.MaxOutputTokenCount);
        
        Assert.Equivalent(ChatResponseFormat.CreateTextFormat(), options.ResponseFormat);
    }

    [Fact]
    public void ToOpenAiChatCompletionOptions_WithMissingConfig_ReturnsAValidOptionsInstance()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/with-name-json.prompt");
        var options = promptFile.ToOpenAiChatCompletionOptions();
        
        Assert.Null(options.Temperature);
        Assert.Null(options.MaxOutputTokenCount);
        
        Assert.Equivalent(ChatResponseFormat.CreateJsonObjectFormat(), options.ResponseFormat);
    }
}