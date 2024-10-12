namespace DotPrompt;

/// <summary>
/// Represents the prompt configuration from a .prompt file
/// </summary>
public class Prompts
{
    /// <summary>
    /// Gets, sets the system prompt
    /// </summary>
    public string? System { get; set; }
    
    /// <summary>
    /// Gets, sets the user prompt template
    /// </summary>
    public required string User { get; set; }
}