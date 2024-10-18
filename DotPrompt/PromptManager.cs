using System.Collections.Concurrent;

namespace DotPrompt;

/// <summary>
/// Manages loading and accessing of .prompt files from a specified directory.
/// </summary>
public class PromptManager
{
    private readonly ConcurrentDictionary<string, PromptFile> _promptFiles = new();
    
    /// <summary>
    /// Creates a new instance of the <see cref="PromptManager"/> using a default instance of the
    /// <see cref="FilePromptStore"/>
    /// </summary>
    public PromptManager() : this(new FilePromptStore()) { }

    /// <summary>
    /// Creates a new instance of the <see cref="PromptManager"/> class which loads the .prompt files from a
    /// specified directory.
    /// </summary>
    public PromptManager(IPromptStore promptStore)
    {
        foreach (var promptFile in promptStore.Load())
        {
            if (!_promptFiles.TryAdd(promptFile.Name, promptFile))
            {
                throw new DotPromptException($"Unable to add prompt file with name '{promptFile.Name}' as a duplicate exists");
            }
        }
    }

    /// <summary>
    /// Retrieves a <see cref="PromptFile"/> by its name
    /// </summary>
    /// <param name="name">The name of the prompt file to retrieve</param>
    /// <returns>The <see cref="PromptFile"/> with the specified name</returns>
    /// <exception cref="DotPromptException">Thrown when no prompt file with the specified name is found</exception>
    public PromptFile GetPromptFile(string name)
    {
        if (_promptFiles.TryGetValue(name, out var promptFile)) 
        {
            return promptFile;
        }

        throw new DotPromptException("No prompt file with that name has been loaded");
    }

    /// <summary>
    /// Lists the names of all loaded prompt files.
    /// </summary>
    /// <returns>An enumerable collection of prompt file names.</returns>
    public IEnumerable<string> ListPromptFileNames()
    {
        return _promptFiles.Keys.Select(k => k);
    }
}