using System.Text.Json;
using Json.More;

namespace DotPrompt;

/// <summary>
/// Represents the output configuration, defining the format and schema related to the output.
/// </summary>
public class Output
{
    private string? _schemaDocument = null;

    /// <summary>
    /// Gets or sets the format of the output. This determines how the output should be structured.
    /// </summary>
    public OutputFormat Format { get; set; } = OutputFormat.Text;

    /// <summary>
    /// Gets or sets the output schema. This property defines the structure or format of the expected output in
    /// detail when the output format is set to JSON Schema.
    /// </summary>
    public object? Schema { get; set; }

    /// <summary>
    /// Converts the Schema object to a JSON schema document string if it hasn't been cached already. The conversion
    /// includes adding a property to disallow additional properties in the schema if not already specified.
    /// </summary>
    /// <returns>
    /// A string representing the JSON schema document. Returns an empty string if the schema is not defined or if
    /// the cached schema document is not available.
    /// </returns>
    public string ToSchemaDocument()
    {
        // TODO: Scan over entire schema document and check for additionalProperties on each item which is an object
        // see https://openai.com/index/introducing-structured-outputs-in-the-api/
        if (_schemaDocument is not null) return _schemaDocument;
        if (Schema is null) return string.Empty;
        
        var schemaObject = JsonDocument.Parse(JsonSerializer.Serialize(Schema));
            
        if (!schemaObject.RootElement.TryGetProperty("additionalProperties", out _))
        {
            var rootNode = schemaObject.RootElement.AsNode();
            rootNode!["additionalProperties"] = false;

            _schemaDocument = rootNode.ToJsonString();
        }
        else
        {
            _schemaDocument = schemaObject.RootElement.ToJsonString();
        }

        return _schemaDocument;
    }
}