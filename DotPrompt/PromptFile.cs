using Fluid;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotPrompt;

/// <summary>
/// Represents the content of a .prompt file
/// </summary>
public class PromptFile
{
    /// <summary>
    /// Provides a collection of valid data types which can be accepted in prompt files
    /// </summary>
    private static readonly string[] ValidDataTypes = ["string", "number", "bool", "datetime", "object"];
    
    /// <summary>
    /// Gets, sets the name of the prompt
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets, sets the configuration to use for the prompt
    /// </summary>
    public PromptConfig Config { get; set; } = new();
    
    /// <summary>
    /// Gets, sets the prompts for the definition
    /// </summary>
    public Prompts? Prompts { get; set; }
    
    /// <summary>
    /// Gets, sets the user and AI interactions for use as few shot prompt examples
    /// </summary>
    public FewShotPair[] FewShots { get; set; } = [];

    /// <summary>
    /// Creates a new <see cref="PromptFile"/> from a given file path
    /// </summary>
    /// <param name="file">The path to the prompt file</param>
    /// <returns>A new <see cref="PromptFile"/> instance</returns>
    /// <exception cref="DotPromptException">Thrown if there is an error parsing the prompt file</exception>
    public static PromptFile FromFile(string file)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists)
        {
            throw new DotPromptException("The specified file does not exist");
        }

        var promptNameFromFile = Path.GetFileNameWithoutExtension(fileInfo.Name);
        using var stream = fileInfo.OpenRead();

        return FromStream(promptNameFromFile, stream);
    }

    /// <summary>
    /// Creates a new <see cref="PromptFile"/> from a given stream
    /// </summary>
    /// <param name="name">The name of the prompt file. If one is not specified in the file then this value will be used</param>
    /// <param name="inputStream">The stream source containing the prompt file definition</param>
    /// <returns>A new <see cref="PromptFile"/> instance</returns>
    /// <exception cref="ArgumentException">Thrown if there is an error with the stream</exception>
    /// <exception cref="DotPromptException">Thrown if there is an error parsing the prompt file</exception>
    public static PromptFile FromStream(string name, Stream inputStream)
    {
        if (!inputStream.CanRead)
        {
            throw new ArgumentException("Stream is not in a readable state", nameof(inputStream));
        }
        
        using var reader = new StreamReader(inputStream);
        
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        PromptFile promptFile;

        try
        {
            promptFile = deserializer.Deserialize<PromptFile>(reader);
        }
        catch (YamlException e)
        {
            throw new DotPromptException("Unable to parse prompt file", e);
        }

        // Check that there are prompts in the file
        if (promptFile.Prompts is null)
        {
            throw new DotPromptException("Unable to extract prompts from prompt file");
        }

        // Validate that there is a user prompt
        if (promptFile.Prompts.User is null)
        {
            throw new DotPromptException("No user prompt template was provided in the prompt file");
        }

        // If the prompt file does not have a name in it, then use the supplied name instead
        if (string.IsNullOrEmpty(promptFile.Name))
        {
            promptFile.Name = name;
        }
        
        // Check the data types of the parameters
        foreach (var (paramName, paramType) in promptFile.Config.Input.Parameters)
        {
            if (!ValidDataTypes.Contains(paramType, StringComparer.OrdinalIgnoreCase))
            {
                throw new DotPromptException(
                    $"The provided data type for '{paramName}' is not a valid type, should be {string.Join(", ", ValidDataTypes)}");
            }
        }
        
        return promptFile;
    }

    /// <summary>
    /// Gets the system prompt
    /// </summary>
    /// <remarks>If the prompt specifies a JSON response format but doesn't use the term JSON in the system prompt then
    /// it is appended as a simple request to ensure the LLM does not reject the request.</remarks>
    /// <returns>A string representing the system prompt, if none was defined then this will return an empty string</returns>
    public string GetSystemPrompt()
    {
        var systemPrompt = Prompts!.System ?? string.Empty;

        if (Config.OutputFormat == OutputFormat.Json &&
            !systemPrompt.Contains("JSON", StringComparison.InvariantCultureIgnoreCase) &&
            !Prompts.User.Contains("JSON", StringComparison.InvariantCultureIgnoreCase))
        {
            const string jsonObjectRequest = "Please provide the response in JSON";

            systemPrompt = string.IsNullOrEmpty(systemPrompt)
                ? jsonObjectRequest
                : systemPrompt + " " + jsonObjectRequest;
        }

        return systemPrompt;
    }

    /// <summary>
    /// Gets the user prompt from the definitions template, substituting the values provided
    /// </summary>
    /// <param name="values">The value needed to populate the template</param>
    /// <returns>The completed user prompt</returns>
    /// <exception cref="DotPromptException">Thrown if there is an error with the template or with the values provided</exception>
    public string GetUserPrompt(IDictionary<string, object>? values)
    {
        var clonedValues = values?.ToDictionary(v => v.Key, v => v.Value)
                           ?? new Dictionary<string, object>();
        
        foreach (var parametersKey in Config.Input.Parameters.Keys)
        {
            if (parametersKey.EndsWith('?') || clonedValues.ContainsKey(parametersKey)) continue;
            
            if (Config.Input.Default.TryGetValue(parametersKey, out var value))
            {
                clonedValues[parametersKey] = value;
            }
            else
            {
                throw new DotPromptException($"Specified values do not contain the required parameter '{parametersKey}' and no default value is provided");
            }
        }
        
        // Validate the data types provided match the parameter type
        foreach (var (param, paramValue) in clonedValues)
        {
            if (!Config.Input.Parameters.TryGetValue(param, out var parameterType)) continue;
        
            switch (parameterType)
            {
                case "string":
                    if (paramValue is not string)
                        throw new DotPromptException($"The value provided for '{param}' is not a valid string");
                    break;
                case "datetime":
                    if (paramValue is not DateTimeOffset)
                        throw new DotPromptException($"The value provided for '{param}' is not a valid DateTimeOffset");
                    break;
                case "bool":
                    if (paramValue is not bool)
                        throw new DotPromptException($"The value provided for '{param}' is not a valid boolean");
                    break;
                case "number":
                    if (paramValue is not (decimal or double or float or sbyte or byte or ushort or short or uint or int or ulong or long))
                        throw new DotPromptException($"The value provided for '{param}' is not a valid numeric type");
                    break;
                case "object":
                    var objectType = paramValue.GetType();
                    if (!objectType.IsClass && objectType is not { IsValueType: true, IsEnum: false, IsPrimitive: false })
                        throw new DotPromptException(
                            $"The value provided for '{param}' is not a valid object type");
                    break;
            }
        }

        var parser = new FluidParser();
        var context = new TemplateContext();

        foreach (var (key, value) in clonedValues)
        {
            context.SetValue(key, value);
        }

        if (!parser.TryParse(Prompts!.User, out var template, out var error))
        {
            throw new DotPromptException($"Unable to parse the user prompt template: {error}");
        }

        return template.Render(context);
    }
}