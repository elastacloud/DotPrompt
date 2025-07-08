namespace DotPrompt;

/// <summary>
/// Provides an implementation of the <see cref="IPromptStore"/> for loading prompt files from the file system.
/// </summary>
public class FilePromptStore : IPromptStore
{
    /// <summary>
    /// Holds the default location for loading prompts from
    /// </summary>
    private const string DefaultPath = "prompts";

    /// <summary>
    /// Holds the location selected by the client to manage prompt files from
    /// </summary>
    internal readonly DirectoryInfo PromptDirectory;
    
    /// <summary>
    /// Creates a new instance of the <see cref="FilePromptStore"/> using the default location
    /// </summary>
    /// <remarks>
    /// Defaults to using the "prompts" folder
    /// </remarks>
    public FilePromptStore() : this(DefaultPath) { }

    /// <summary>
    /// Creates a new instance of the <see cref="FilePromptStore"/> using the provided location
    /// </summary>
    /// <param name="path">The path to the directory containing the prompt files</param>
    /// <exception cref="ArgumentException">Thrown if the specified location does not exist</exception>
    public FilePromptStore(string path)
    {
        PromptDirectory = new DirectoryInfo(path);

        if (!PromptDirectory.Exists)
        {
            throw new ArgumentException("The specified path does not exist", nameof(path));
        }
    }
    
    /// <summary>
    /// Iterates recursively through the path specified during creation of the instance and generates
    /// a prompt file for each file encountered. Non-prompt files are ignored.
    /// </summary>
    /// <returns>A collect of <see cref="PromptFile"/> instances generated from files in the directory</returns>
    public IEnumerable<PromptFile> Load()
    {
        var options = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
            IgnoreInaccessible = true
        };

        foreach (var file in PromptDirectory.EnumerateFiles("*.prompt", options))
        {
            var promptFile = PromptFile.FromFile(file.FullName);
            yield return promptFile;
        }
    }

    /// <summary>
    /// Saves a <see cref="PromptFile"/> to the specified location.
    /// </summary>
    /// <param name="promptFile">The prompt file to save.</param>
    /// <param name="name">The name to save the prompt as</param>
    public void Save(PromptFile promptFile, string? name = null)
    {
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(promptFile.Name))
        {
            throw new ArgumentException("A name must be provided for the prompt file");
        }
        
        if (string.IsNullOrEmpty(name))
        {
            name = promptFile.Name;
        }

        if (name.EndsWith(".prompt", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Replace(".prompt", "", StringComparison.OrdinalIgnoreCase);
        }

        var outputFile = new FileInfo(Path.Join(PromptDirectory.FullName, $"{name}.prompt"));
        promptFile.ToFile(outputFile.FullName);
    }
}