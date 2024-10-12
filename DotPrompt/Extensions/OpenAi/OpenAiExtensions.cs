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

        // If the prompt file provides any few shot prompts then include these first
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
            messages.Add(new SystemChatMessage(promptFile.GetSystemPrompt()));
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
        var chatCompletionOptions = new ChatCompletionOptions
        {
            ResponseFormat = promptFile.Config.OutputFormat == OutputFormat.Json
                ? ChatResponseFormat.CreateJsonObjectFormat()
                : ChatResponseFormat.CreateTextFormat()
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