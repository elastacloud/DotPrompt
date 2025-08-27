using System.Collections.Concurrent;

namespace DotPrompt;

/// <summary>
/// Identifies a .prompt file uniquely using its name and version
/// </summary>
/// <param name="Name">The name of the prompt file</param>
/// <param name="Version">The prompt file version</param>
public record PromptFileIdentifier(string Name, int Version);

/// <summary>
/// Manages loading and accessing of .prompt files from a specified directory.
/// </summary>
public class PromptManager : IPromptManager
{
    private readonly ConcurrentDictionary<PromptFileIdentifier, PromptFile> _promptFiles = new();
    
    /// <summary>
    /// Creates a new instance of the <see cref="PromptManager"/> using a default instance of the
    /// <see cref="FilePromptStore"/>
    /// </summary>
    public PromptManager() : this(new FilePromptStore()) { }
    
    /// <summary>
    /// Creates a new instance of the <see cref="PromptManager"/> using the provided path for an
    /// instance of the <see cref="FilePromptStore"/>
    /// </summary>
    /// <param name="path">The path to the directory containing the prompt files</param>
    public PromptManager(string path) : this(new FilePromptStore(path)) { }

    /// <summary>
    /// Creates a new instance of the <see cref="PromptManager"/> class which loads the .prompt files from a
    /// specified directory.
    /// </summary>
    public PromptManager(IPromptStore promptStore)
    {
        foreach (var promptFile in promptStore.Load())
        {
            if (!_promptFiles.TryAdd(new PromptFileIdentifier(promptFile.Name, promptFile.Version), promptFile))
            {
                throw new DotPromptException($"Unable to add prompt file with name '{promptFile.Name}' and version {promptFile.Version} as a duplicate exists");
            }
        }
    }

    /// <summary>
    /// Retrieves a <see cref="PromptFile"/> by its name. If multiple versions of the prompt file are found, the
    /// latest version is returned.
    /// </summary>
    /// <param name="name">The name of the prompt file to retrieve</param>
    /// <returns>The <see cref="PromptFile"/> with the specified name</returns>
    /// <exception cref="DotPromptException">Thrown when no prompt file with the specified name is found</exception>
    public PromptFile GetPromptFile(string name)
    {
        var promptFilesWithName = _promptFiles
            .Where(kvp => kvp.Key.Name == name)
            .OrderByDescending(kvp => kvp.Key.Version)
            .ToList();

        return promptFilesWithName.Count == 0
            ? throw new DotPromptException("No prompt file with that name has been loaded")
            : promptFilesWithName[0].Value;
    }

    /// <summary>
    /// Retrieves a <see cref="PromptFile"/> by its name and version.
    /// </summary>
    /// <param name="name">The name of the prompt file to retrieve</param>
    /// <param name="version">The version of the prompt file to retrieve</param>
    /// <returns>The <see cref="PromptFile"/> with the specified name and version</returns>
    /// <exception cref="DotPromptException">Thrown when no prompt file with the specified name and version is found</exception>
    public PromptFile GetPromptFile(string name, int version)
    {
        return _promptFiles.TryGetValue(new PromptFileIdentifier(name, version), out var promptFile)
            ? promptFile
            : throw new DotPromptException("No prompt file with that name and version has been loaded");
    }
    
    /// <summary>
    /// Lists the names of all loaded prompt files.
    /// </summary>
    /// <returns>An enumerable collection of prompt file names.</returns>
    public IEnumerable<string> ListPromptFileNames()
    {
        return _promptFiles
            .DistinctBy(kvp => kvp.Key.Name)
            .OrderBy(kvp => kvp.Key.Name)
            .Select(kvp => $"{kvp.Key.Name}");
    }

    /// <summary>
    /// Lists the names of all loaded prompts with their versions.
    /// </summary>
    /// <returns>An enumerable collection of prompt file names and versions.</returns>
    public IEnumerable<string> ListPromptFileNamesWithVersions()
    {
        return _promptFiles
            .OrderBy(kvp => kvp.Key.Name)
            .ThenByDescending(kvp => kvp.Key.Version)
            .Select(kvp => $"{kvp.Key.Name}:{kvp.Key.Version}");
    }
}