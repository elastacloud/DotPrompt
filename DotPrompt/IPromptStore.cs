namespace DotPrompt;

/// <summary>
/// Defines method for loading and managing prompt files.
/// </summary>
public interface IPromptStore
{
    /// <summary>
    /// Loads and returns a collection of prompt files.
    /// </summary>
    /// <returns>A collection of prompt files.</returns>
    IEnumerable<PromptFile> Load();

    /// <summary>
    /// Saves the specified prompt file.
    /// </summary>
    /// <param name="promptFile">The prompt file to save.</param>
    /// <param name="name">The name to save the prompt as.</param>
    void Save(PromptFile promptFile, string? name);
}