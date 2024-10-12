namespace DotPrompt;

/// <summary>
/// Provides the configuration information for the prompt
/// </summary>
public class PromptConfig
{
    /// <summary>
    /// Gets, sets the schema for the prompt
    /// </summary>
    public InputSchema Input { get; set; } = new();

    /// <summary>
    /// Gets, sets the format for the response from the model
    /// </summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Text;
    
    /// <summary>
    /// Gets, sets the optional temperature value for the model
    /// </summary>
    public float? Temperature { get; set; }
    
    /// <summary>
    /// Gets, sets the optional maximum number of tokens to use for the prompt
    /// </summary>
    public int? MaxTokens { get; set; }
}