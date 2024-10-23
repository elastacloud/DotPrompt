namespace DotPrompt;

/// <summary>
/// Defines methods for working with prompt files through a prompt manager instance
/// </summary>
public interface IPromptManager
{
    /// <summary>
    /// Retrieves a <see cref="PromptFile"/> by its name
    /// </summary>
    /// <param name="name">The name of the prompt file to retrieve</param>
    /// <returns>The <see cref="PromptFile"/> with the specified name</returns>
    /// <exception cref="DotPromptException">Thrown when no prompt file with the specified name is found</exception>
    PromptFile GetPromptFile(string name);

    /// <summary>
    /// Lists the names of all loaded prompt files
    /// </summary>
    /// <returns>An enumerable collection of prompt file names</returns>
    IEnumerable<string> ListPromptFileNames();
}