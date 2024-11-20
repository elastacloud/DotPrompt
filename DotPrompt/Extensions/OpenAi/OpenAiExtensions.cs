using OpenAI.Chat;

namespace DotPrompt.Extensions.OpenAi;

/// <summary>
/// Provides extension methods for converting <see cref="PromptFile"/> instances to OpenAI compatible types.
/// </summary>
public static class OpenAiExtensions
{
    /// <summary>
    /// Converts a <see cref="PromptFile"/> instance to a collection of <see cref="ChatMessage"/> objects.
    /// </summary>
    /// <param name="promptFile">The <see cref="PromptFile"/> instance containing prompt definitions.</param>
    /// <param name="values">A dictionary of values to be substituted in the user prompt template.</param>
    /// <returns>An enumerable collection of <see cref="ChatMessage"/> objects.</returns>
    public static IEnumerable<ChatMessage> ToOpenAiChatMessages(this PromptFile promptFile,
        IDictionary<string, object>? values)
    {
        var messages = new List<ChatMessage>();

        // If the prompt file provides any few shot prompts, then include these first
        if (promptFile.FewShots.Length != 0)
        {
            foreach (var fewShot in promptFile.FewShots)
            {
                messages.Add(new UserChatMessage(fewShot.User));
                messages.Add(new AssistantChatMessage(fewShot.Response));
            }
        }

        if (!string.IsNullOrEmpty(promptFile.Prompts!.System))
        {
            messages.Add(new SystemChatMessage(promptFile.GetSystemPrompt(values)));
        }
        
        messages.Add(new UserChatMessage(promptFile.GetUserPrompt(values)));

        return messages;
    }

    /// <summary>
    /// Converts a <see cref="PromptFile"/> instance to an <see cref="ChatCompletionOptions"/> object.
    /// </summary>
    /// <param name="promptFile">The <see cref="PromptFile"/> instance containing configuration and prompt definitions.</param>
    /// <returns>A <see cref="ChatCompletionOptions"/> object configured based on the <see cref="PromptFile"/> instance.</returns>
    public static ChatCompletionOptions ToOpenAiChatCompletionOptions(this PromptFile promptFile)
    {
        var chatResponseFormat = promptFile.Config.OutputFormat switch
        {
            OutputFormat.Text => ChatResponseFormat.CreateTextFormat(),
            OutputFormat.Json => ChatResponseFormat.CreateJsonObjectFormat(),
            OutputFormat.JsonSchema when promptFile.Config.Output?.Schema is not null =>
                ChatResponseFormat.CreateJsonSchemaFormat(
                    promptFile.Name,
                    BinaryData.FromString(promptFile.Config.Output.ToSchemaDocument()),
                    jsonSchemaIsStrict: true),
            OutputFormat.JsonSchema when promptFile.Config.Output?.Schema is null =>
                throw new DotPromptException("A valid schema was not provided to be used with the JsonSchema response type"),
            _ => throw new DotPromptException("The requested output format is not available")
        };
        
        var chatCompletionOptions = new ChatCompletionOptions
        {
            ResponseFormat = chatResponseFormat
        };

        if (promptFile.Config.Temperature is not null)
        {
            chatCompletionOptions.Temperature = promptFile.Config.Temperature;
        }

        if (promptFile.Config.MaxTokens is not null)
        {
            chatCompletionOptions.MaxOutputTokenCount = promptFile.Config.MaxTokens;
        }

        return chatCompletionOptions;
    }
}