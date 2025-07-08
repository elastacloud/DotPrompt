using System.Text.Json;

namespace DotPrompt.Tests;

public class OutputTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("{}", "{\"additionalProperties\":false}")]
    [InlineData("{\"additionalProperties\":true}", "{\"additionalProperties\":true}")]
    public void ToSchemaDocument_VariousScenarios_ReturnsExpectedResult(string? schemaJson, string expectedJson)
    {
        // Arrange
        var output = new Output();

        if (schemaJson != null)
        {
            var schema = JsonDocument.Parse(schemaJson);
            output.Schema = schema.Deserialize<Dictionary<string, object>>();
        }

        // Act
        var result = output.ToSchemaDocument();

        // Assert
        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public void ToSchemaDocument_WhenCalledTwice_ReturnsSameResult()
    {
        const string schemaJson = """{"additionalProperties":true}""";
        var output = new Output
        {
            Schema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson)
        };
        
        var result1 = output.ToSchemaDocument();
        var result2 = output.ToSchemaDocument();
        
        Assert.Equal(result1, result2);
    }
}