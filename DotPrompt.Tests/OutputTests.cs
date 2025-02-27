using System.Text.Json;
using DotPrompt;

public class OutputTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("{}", "{\"additionalProperties\": false}")]
    [InlineData("{\"additionalProperties\":true}", "{\"additionalProperties\":true}")]
    public void ToSchemaDocument_VariousScenarios_ReturnsExpectedResult(string schemaJson, string expectedJson)
    {
        // Arrange
        var output = new Output();

        if (schemaJson != null)
        {
            var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);
            output.Schema = schema;
        }

        // Act
        var result = output.ToSchemaDocument();

        // Assert
        Assert.Equal(expectedJson, result);
    }
}