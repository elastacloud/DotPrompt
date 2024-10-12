namespace DotPrompt;

/// <summary>
/// Represents an exchange between the user and the AI
/// </summary>
public class FewShotPair
{
    /// <summary>
    /// Gets, sets the user request to the AI
    /// </summary>
    public required string User { get; set; }
    
    /// <summary>
    /// Gets, sets the AI response to the user
    /// </summary>
    public required string Response { get; set; }
}