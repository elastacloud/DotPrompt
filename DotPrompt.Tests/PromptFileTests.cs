namespace DotPrompt.Tests;

public class PromptFileTests
{
    [Fact]
    public void FromFile_BasicPrompt_ProducesValidPromptFile()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");

        var expectedParameters = new Dictionary<string, string>
        {
            { "country", "string" },
            { "style?", "string" }
        };

        var expectedDefaults = new Dictionary<string, object>
        {
            { "country", "Malta" }
        };
        
        Assert.Equal("basic", promptFile.Name);
        
        Assert.NotNull(promptFile.Config);
        Assert.Equal(OutputFormat.Text, promptFile.Config.OutputFormat);
        Assert.Equal(500, promptFile.Config.MaxTokens);
        Assert.NotNull(promptFile.Config.Temperature);
        Assert.Equal(0.9, promptFile.Config.Temperature.Value, 1e-2);
        
        Assert.Equal(expectedParameters, promptFile.Config.Input.Parameters);
        Assert.Equal(expectedDefaults, promptFile.Config.Input.Default);

        Assert.NotNull(promptFile.Prompts);
        Assert.StartsWith("You are a helpful AI assistant that enjoys making penguin related puns.",
            promptFile.Prompts.System);
        Assert.StartsWith("I am looking at going on holiday to {{ country }}", promptFile.Prompts.User);
        
        Assert.Empty(promptFile.FewShots);
    }

    [Fact]
    public void FromFile_WithNameSetting_UsesNameFromConfigurationInsteadOfFile()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/with-name-json.prompt");
        
        Assert.Equal("Example with Name", promptFile.Name);
        Assert.Equal(OutputFormat.Json, promptFile.Config.OutputFormat);
        Assert.Null(promptFile.Config.Temperature);
        Assert.Null(promptFile.Config.MaxTokens);
        Assert.Null(promptFile.Prompts!.System);
    }

    [Fact]
    public void FromFile_WithFewShotPrompts_CorrectlyExtractsValuesFromFile()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic-fsp.prompt");
        
        Assert.NotEmpty(promptFile.FewShots);
        Assert.Equal(3, promptFile.FewShots.Length);
        
        Assert.Equal("What is Bluetooth", promptFile.FewShots[0].User);
        Assert.Equal(
            "AI is used in virtual assistants like Siri and Alexa, which understand and respond to voice commands.",
            promptFile.FewShots[2].Response);
    }

    [Fact]
    public void GetSystemPrompt_WhenProvided_ReturnsTheCorrectPrompt()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");

        var systemPrompt = promptFile.GetSystemPrompt();
        Assert.Equal("You are a helpful AI assistant that enjoys making penguin related puns. You should work as many into your response as possible\n", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_WhenNoneProvided_ReturnsAnEmptyString()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/with-name.prompt");

        var systemPrompt = promptFile.GetSystemPrompt();
        Assert.Equal(string.Empty, systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_WhenJsonIsRequestedAndMessagesDoNotStateJson_SystemPromptIsCreated()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/with-name-json.prompt");

        var systemPrompt = promptFile.GetSystemPrompt();
        Assert.Equal("Please provide the response in JSON", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_WhenJsonIsRequestedAndMessagesDoNotStateJson_SystemPromptIsModified()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/json-missing-messages.prompt");

        var systemPrompt = promptFile.GetSystemPrompt();
        Assert.Equal("You are the voice of the guide, you should be authoritative and informative and appeal to a galactic audience. Please provide the response in JSON", systemPrompt);
    }

    [Fact]
    public void FromFile_InvalidPath_ThrowsException()
    {
        Action act = () => PromptFile.FromFile("SamplePrompts/does-not-exist.prompt");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("The specified file does not exist", exception.Message);
    }

    [Fact]
    public void FromFile_InvalidContent_ThrowsException()
    {
        Action act = () => PromptFile.FromFile("SamplePrompts/invalid-file.prompt");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("Unable to extract prompts from prompt file", exception.Message);
    }

    [Fact]
    public void FromFile_MissingUserPrompt_ThrowsException()
    {
        Action act = () => PromptFile.FromFile("SamplePrompts/missing-user-prompt.prompt");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("No user prompt template was provided in the prompt file", exception.Message);
    }

    [Fact]
    public void FromFile_InvalidYamlContent_ThrowsException()
    {
        Action act = () => PromptFile.FromFile("SamplePrompts/basic-broken.prompt");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("Unable to parse prompt file", exception.Message);
    }

    [Fact]
    public void FromFile_InvalidParameterType_ThrowsException()
    {
        Action act = () => PromptFile.FromFile("SamplePrompts/invalid-params.prompt");

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains($"The provided data type for 'oops' is not a valid type, should be string, number, bool, datetime, object",
            exception.Message);
    }

    [Fact]
    public void FromStream_WithUnreadableStream_ThrowsException()
    {
        using var ms = new MemoryStream();
        var inputStream = new MockStream(ms, false, true);

        Action act = () => PromptFile.FromStream("test", inputStream);

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Stream is not in a readable state", exception.Message);
    }

    [Fact]
    public void GenerateUserPrompt_UsingDefaults_CorrectlyGeneratesPromptFromTemplate()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        var userPrompt = promptFile.GetUserPrompt(null);

        Assert.Equal(
            "I am looking at going on holiday to Malta and would like to know more about it, what can you tell me?\n",
            userPrompt
        );
    }

    [Fact]
    public void GenerateUserPrompt_WithParameterValues_CorrectlyGeneratesPromptFromTemplate()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>{ { "country", "Antarctica" } });

        Assert.Equal(
            "I am looking at going on holiday to Antarctica and would like to know more about it, what can you tell me?\n",
            userPrompt
        );
    }

    [Fact]
    public void GenerateUserPrompt_WithUnusedValues_CorrectlyGeneratesPromptFromTemplate()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
        {
            { "country", "Antarctica" },
            { "not-used", "This isn't used" }
        });

        Assert.Equal(
            "I am looking at going on holiday to Antarctica and would like to know more about it, what can you tell me?\n",
            userPrompt
        );
    }

    [Fact]
    public void GenerateUserPrompt_WithOptionalValue_CorrectlyGeneratesPromptFromTemplate()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/basic.prompt");
        var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
        {
            { "country", "Antarctica" },
            { "style", "Pirate" }
        });

        Assert.Equal(
            "I am looking at going on holiday to Antarctica and would like to know more about it, what can you tell me?\nCan you answer in the style of a Pirate\n",
            userPrompt
        );
    }

    [Fact]
    public void GenerateUserPrompt_WithMissingValues_ThrowsAnException()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/required-parameters.prompt");
        var act = () => promptFile.GetUserPrompt(null);

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("Specified values do not contain the required parameter 'name'", exception.Message);
    }

    [Fact]
    public void GenerateUserPrompt_WithMissingValuesFromDictionary_ThrowsAnException()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/required-parameters.prompt");
        var act = () => promptFile.GetUserPrompt(new Dictionary<string, object>{ { "not-this", "This isn't it" } });

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("Specified values do not contain the required parameter 'name'", exception.Message);
    }

    [Fact]
    public void GenerateUserPrompt_WithInvalidTemplate_ThrowsAnException()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/invalid-prompt.prompt");
        var act = () => promptFile.GetUserPrompt(null);

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains("Unable to parse the user prompt template", exception.Message);
    }

    [Fact]
    public void GenerateUserPrompt_WithAllParameterValues_GeneratesValidPrompt()
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/param-types.prompt");

        var expectedPrompt =
            "Parameter 1: Arthur Dent\nParameter 2: 42\nParameter 3: true\nParameter 4: 2024-01-02 03:04:05Z\nParameter 5: { SEP = True }\nParameter 6: Hello : 12";

        var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
        {
            { "param1", "Arthur Dent" },
            { "param2", 42 },
            { "param3", true },
            { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
            { "param5", new { SEP = true } },
            { "param6", new TestStruct{ Item1 = "Hello", Item2 = 12 } }
        });
        
        Assert.Equal(expectedPrompt, userPrompt);
    }

    [Theory]
    [MemberData(nameof(InvalidTypeData))]
    public void GenerateUserPrompt_WithInvalidStringParameterValueType_ThrowsException(Dictionary<string, object> parameterValues, string expectedError)
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/param-types.prompt");

        var act = () => promptFile.GetUserPrompt(parameterValues);

        var exception = Assert.Throws<DotPromptException>(act);
        Assert.Contains(expectedError, exception.Message);
    }

    [Theory]
    [MemberData(nameof(NumericData))]
    public void GenerateUserPrompt_WithNumericValueTypes_GeneratesValidPrompt(object value, string expectedPrompt)
    {
        var promptFile = PromptFile.FromFile("SamplePrompts/numeric-types.prompt");

        var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object> { { "param1", value } });
        
        Assert.Equal(expectedPrompt, userPrompt);
    }

    public static IEnumerable<object[]> NumericData =>
        new List<object[]>
        {
            new object[] { 3d, "Pass" },
            new object[] { (double)4, "Pass" },
            new object[] { (float)5, "Pass" },
            new object[] { (sbyte)6, "Pass" },
            new object[] { (byte)7, "Pass" },
            new object[] { (short)8, "Pass" },
            new object[] { (ushort)9, "Pass" },
            new object[] { 10, "Pass" },
            new object[] { (uint)11, "Pass" },
            new object[] { (long)12, "Pass" },
            new object[] { (ulong)13, "Pass" },
            new object[] { 2, string.Empty }
        };

    public static IEnumerable<object[]> InvalidTypeData =>
        new List<object[]>
        {
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", 1 },
                    { "param2", 42 },
                    { "param3", true },
                    { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
                    { "param5", new { SEP = true } },
                    { "param6", new TestStruct { Item1 = "Hello", Item2 = 12 } }
                },
                "not a valid string"
            },
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", "Arthur Dent" },
                    { "param2", "42" },
                    { "param3", true },
                    { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
                    { "param5", new { SEP = true } },
                    { "param6", new TestStruct { Item1 = "Hello", Item2 = 12 } }
                },
                "not a valid numeric type"
            },
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", "Arthur Dent" },
                    { "param2", 42 },
                    { "param3", "nope" },
                    { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
                    { "param5", new { SEP = true } },
                    { "param6", new TestStruct { Item1 = "Hello", Item2 = 12 } }
                },
                "not a valid boolean"
            },
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", "Arthur Dent" },
                    { "param2", 42 },
                    { "param3", true },
                    { "param4", "2024-01-02" },
                    { "param5", new { SEP = true } },
                    { "param6", new TestStruct { Item1 = "Hello", Item2 = 12 } }
                },
                "not a valid DateTimeOffset"
            },
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", "Arthur Dent" },
                    { "param2", 42 },
                    { "param3", true },
                    { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
                    { "param5", 10 },
                    { "param6", new TestStruct { Item1 = "Hello", Item2 = 12 } }
                },
                "not a valid object type"
            },
            new object[]
            {
                new Dictionary<string, object>
                {
                    { "param1", "Arthur Dent" },
                    { "param2", 42 },
                    { "param3", true },
                    { "param4", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero) },
                    { "param5", new { SEP = true } },
                    { "param6", 10 }
                },
                "not a valid object type"
            }
        };
}

internal struct TestStruct
{
    public string Item1 { get; set; }
    
    public int Item2 { get; set; }

    public override string ToString()
    {
        return $"{Item1} : {Item2}";
    }
}