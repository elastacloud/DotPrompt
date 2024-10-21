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
}