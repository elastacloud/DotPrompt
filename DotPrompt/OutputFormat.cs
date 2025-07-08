namespace DotPrompt;

/// <summary>
/// Defines the permitted set of formats to use for the model output
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Defines the output format as text only
    /// </summary>
    Text,
    
    /// <summary>
    /// Defines that the model should return its response as a JSON object
    /// </summary>
    Json,

    /// <summary>
    /// Defines the output format as JSON with a specified schema
    /// </summary>
    JsonSchema
}