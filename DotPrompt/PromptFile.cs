using System.Globalization;
using System.Text.RegularExpressions;
using Fluid;
using Json.Schema;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotPrompt;

/// <summary>
/// Represents the content of a .prompt file
/// </summary>
public partial class PromptFile
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
    /// Gets, sets the name of the model (or deployment) the prompt should be executed using
    /// </summary>
    public string? Model { get; set; }

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

        // If the prompt output configuration is null then create a new one and set the output format. This is to handle
        // instances where the output format is slightly older and is set at the top level
        promptFile.Config.Output ??= new Output
        {
            Format = promptFile.Config.OutputFormat
        };
        
        // Ensure the config output format and the config.output format are the same
        promptFile.Config.OutputFormat = promptFile.Config.Output.Format;

        // If an output schema has been defined then check to make sure it generates a valid JSON schema
        if (promptFile.Config.Output.Schema is not null)
        {
            try
            {
                JsonSchema.FromText(promptFile.Config.Output.ToSchemaDocument());
            }
            catch (Exception e)
            {
                throw new DotPromptException("Invalid output schema specified", e);
            }
        }
        
        // Ensure the name conforms to standards
        var originalName = promptFile.Name;
        promptFile.Name = CleanName(promptFile.Name);
        if (string.IsNullOrEmpty(promptFile.Name))
        {
            throw new DotPromptException($"The provided name '{originalName}' once cleaned results in an empty string");
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
    /// Serializes the current <see cref="PromptFile"/> instance to the specified output stream.
    /// </summary>
    /// <param name="outputStream">The stream to which the <see cref="PromptFile"/> will be serialized</param>
    /// <exception cref="DotPromptException">Thrown if the output stream is not writable</exception>
    public void ToStream(Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);

        if (!outputStream.CanWrite)
        {
            throw new DotPromptException("Unable to use stream as it is not writeable");
        }
        
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDefaultScalarStyle(ScalarStyle.Any)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        using var writer = new StreamWriter(outputStream, leaveOpen: true);
        serializer.Serialize(writer, this, typeof(PromptFile));
    }

    /// <summary>
    /// Serializes the current <see cref="PromptFile"/> instance to the specified file path.
    /// </summary>
    /// <param name="file">The path to the file where the <see cref="PromptFile"/> will be serialized</param>
    /// <exception cref="DotPromptException">Thrown if there is an error during file writing</exception>
    public void ToFile(string file)
    {
        using var ms = new MemoryStream();
        ToStream(ms);
        
        ms.Seek(0, SeekOrigin.Begin);
        
        using var outputStream = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None);
        ms.CopyTo(outputStream);
    }

    /// <summary>
    /// Gets the system prompt
    /// </summary>
    /// <param name="values">The value needed to populate the template</param>
    /// <remarks>If the prompt specifies a JSON response format but doesn't use the term JSON in the system prompt then
    /// it is appended as a simple request to ensure the LLM does not reject the request.</remarks>
    /// <returns>A string representing the system prompt, if none was defined, then this will return an empty string</returns>
    public string GetSystemPrompt(IDictionary<string, object>? values)
    {
        var systemPrompt = Prompts!.System ?? string.Empty;

        var clonedValues = CloneAndValidateInputParameters(values);

        if (Config.OutputFormat == OutputFormat.Json &&
            !systemPrompt.Contains("JSON", StringComparison.InvariantCultureIgnoreCase) &&
            !Prompts.User.Contains("JSON", StringComparison.InvariantCultureIgnoreCase))
        {
            const string jsonObjectRequest = "Please provide the response in JSON";

            systemPrompt = string.IsNullOrEmpty(systemPrompt)
                ? jsonObjectRequest
                : systemPrompt + " " + jsonObjectRequest;
        }

        return GeneratePrompt(systemPrompt, clonedValues);
    }

    /// <summary>
    /// Gets the user prompt from the definition's template, substituting the values provided
    /// </summary>
    /// <param name="values">The value needed to populate the template</param>
    /// <returns>The completed user prompt</returns>
    /// <exception cref="DotPromptException">Thrown if there is an error with the template or with the values provided</exception>
    public string GetUserPrompt(IDictionary<string, object>? values)
    {
        var clonedValues = CloneAndValidateInputParameters(values);
        return GeneratePrompt(Prompts!.User, clonedValues);
    }

    /// <summary>
    /// Takes an input prompt template and generates the rendered template using the parameter values provided
    /// </summary>
    /// <param name="promptTemplate">The prompt template to parse and render</param>
    /// <param name="values">The values needed to populate the template</param>
    /// <returns>A rendered template</returns>
    /// <exception cref="DotPromptException">Thrown if the template syntax is invalid</exception>
    private static string GeneratePrompt(string promptTemplate, Dictionary<string, object> values)
    {
        var parser = new FluidParser();
        var context = new TemplateContext();

        foreach (var (key, value) in values)
        {
            context.SetValue(key, value);
        }

        if (!parser.TryParse(promptTemplate, out var template, out var error))
        {
            throw new DotPromptException($"Unable to parse the prompt template: {error}");
        }

        return template.Render(context);
    }

    /// <summary>
    /// Creates a clone of the input parameters so that the provided values are not modified, causing unexpected
    /// behaviour in the client code. The values are then validated against the prompt files parameters.
    /// </summary>
    /// <param name="values">The values to clone and validate</param>
    /// <returns>The validated parameter values</returns>
    /// <exception cref="DotPromptException">
    /// Thrown if the user-provided values contain an issue such as not containing required parameters, or the value
    /// not conforming to the input parameters type.
    /// </exception>
    private Dictionary<string, object> CloneAndValidateInputParameters(IDictionary<string, object>? values)
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

        return clonedValues;
    }

    /// <summary>
    /// Cleans the given name by removing invalid characters and normalising whitespace
    /// </summary>
    /// <param name="name">The name to be cleaned</param>
    /// <returns>The cleaned name, with invalid characters removed and whitespace normalised to dashes</returns>
    private static string CleanName(string name)
    {
        var invalidCharsRegex = InvalidCharactersRegex();
        var multipleSpacesRegex = MultipleSpacesRegex();

        var strippedName = invalidCharsRegex.Replace(name, "");
        var trimmedName = multipleSpacesRegex.Replace(strippedName, "-")
            .Trim('-')
            .ToLower(CultureInfo.InvariantCulture);
        
        return trimmedName;
    }

    [GeneratedRegex(@"([^A-Za-z0-9 \-\r\n]*)", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex InvalidCharactersRegex();
    
    [GeneratedRegex(@"[\s\r\n]+", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();
}