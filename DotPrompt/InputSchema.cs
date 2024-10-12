namespace DotPrompt;

/// <summary>
/// Defines the schema of the prompt
/// </summary>
public class InputSchema
{
    /// <summary>
    /// Gets, sets the parameters to be used to fill in the prompt template
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Gets, sets the default values to use for the parameters if the user provides none
    /// </summary>
    public Dictionary<string, object> Default { get; set; } = new();
}