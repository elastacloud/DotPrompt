# DotPrompt

[![NuGet version](https://badge.fury.io/nu/DotPrompt.svg)](https://badge.fury.io/nu/DotPrompt)
[![codecov](https://codecov.io/github/elastacloud/DotPrompt/graph/badge.svg?token=hTfjzLIEsM)](https://codecov.io/github/elastacloud/DotPrompt)

DotPrompt is a simple library which allows you to build prompts using a configuration-based syntax, without needing to embed them into your application. It supports templating for prompts through the [Fluid](https://github.com/sebastienros/fluid) templating language, allowing you to re-use the same prompt and pass in different values at runtime.

A prompt file is simply any file ending with a `.prompt` extension. The actual file itself is a YAML configuration file, and the extension allows the library to quickly identify the file for its intended purpose.

## Note for JetBrains IDE users

There is a [known issue](https://youtrack.jetbrains.com/issue/IJPL-162378) with `.prompt` files causing unusual behaviour in tools such as Rider and IntelliJ. You can work around this by either disabling the Terminal plug-in or by using a different editor to modify the files.

## The prompt file

A prompt file's contents contain some top-level identification properties, followed by configuration information and then finally the prompts.

A complete prompt file would look like this.

```yaml
name: Example
model: gpt-4o
config:
  outputFormat: text
  temperature: 0.9
  maxTokens: 500
  input:
    parameters:
      topic: string
      style?: string
    default:
      topic: social media
prompts:
  system: |
    You are a helpful research assistant who will provide descriptive responses for a given topic and how it impacts society
  user: |
    Explain the impact of {{ topic }} on how we engage with technology as a society
    {% if style -%}
    Can you answer in the style of a {{ style }}
    {% endif -%}
fewShots:
  - user: What is Bluetooth
    response: Bluetooth is a short-range wireless technology standard that is used for exchanging data between fixed and mobile devices over short distances and building personal area networks.
  - user: How does machine learning differ from traditional programming?
    response: Machine learning allows algorithms to learn from data and improve over time without being explicitly programmed.
  - user: Can you provide an example of AI in everyday life?
    response: AI is used in virtual assistants like Siri and Alexa, which understand and respond to voice commands.
```

### Name

The `name` is optional in the configuration, if it's not provided then the name is taken from the file name minus the extension. So a file called `gen-lookup-code.prompt` would get the name `gen-lookup-code`. This doesn't play a role in the generation of the prompts themselves (though future updates might), but allows you to identify the prompt source when logging, and to select the prompt from the prompt manager.

If you use this property then when the file is loaded the name is converted to lowercase and spaces are replaced with hyphens. So a name of `My cool Prompt` would become `my-cool-prompt`. This is done to make sure the name is easily accessible from the code.

### Model

This is another optional item in the configuration, but it provides information to the user of the prompt file which model (or deployment for Azure Open AI) it should use. As this can be null if not specified this the consumer should make sure to check before usage. For example:

```csharp
var model = promptFile.Model ?? "my-default";
```

Using this option though allows the prompt engineer to be very explicit about which model they intended to be used to provide the best results.

### Config

The `config` section has some top level items which are provided for the client to use in their LLM calls to set options on each call. The `outputFormat` property takes a value of either `text` or `json` depending on how the LLM is intended to respond to the request. If specifying `json` then some LLMs require either the system or user prompt to state that the expected output is JSON as well. If the library does not detect the term `JSON` in the prompt then it will append a small statement to the system prompt requesting for the response to be in JSON format.

### Input

The `input` section contains details about the parameters being provided to the prompts. These aren't required and you can create prompts which don't have any values being passed in at all. But if you do then these are what you need.

#### Parameters

Under `input` is the `parameters` section which contains a list of key-value pairs where the key is the name of the parameter, and the value is it's type. If you suffix the parameter name with a question mark (e.g. `style?`) then it is considered to be an optional parameter and will not error if you do not provide a value for it.

The supported types are:

| Parameter Type | Dotnet Type                                                                                                                                                                                  | C# Equivalent                                                                                            |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| string         | System.String                                                                                                                                                                                | string                                                                                                   |
| bool           | System.Boolean                                                                                                                                                                               | bool                                                                                                     |
| datetime       | System.DateTimeOffset                                                                                                                                                                        | System.DateTimeOffset                                                                                    |
| number         | System.Byte<br/>System.SByte<br/>System.UInt16<br/>System.Int16<br/>System.UInt32<br/>System.Int32<br/>System.UInt64<br/>System.Int64<br/>System.Single<br/>System.Double<br/>System.Decimal | byte<br/>sbyte<br/>ushort<br/>short<br/>uint<br/>int<br/>ulong<br/>long<br/>float<br/>double<br/>decimal |
| object         | System.Object                                                                                                                                                                                | object                                                                                                   |

The first 4 are used as provided. Objects which are passed to the prompt will have their `ToString` method called to be used in the prompt.

The `datetime` type can either be displayed with it's default `ToString` representation, or you can use Fluid's filters to specify it's [format](https://deanebarker.net/tech/fluid/filters/misc/#h19), change timezone, and more.

If you provide a value for a parameter which does not conform to the type specified then an error would be thrown.

#### Defaults

Also in `input` is the `default` section. This section allows you to specify default values for any of the parameters. So if the parameter is not provided in your application then the default value will be used instead.

### Prompts

The `prompts` section contains the templates for the system and user prompts. Whilst the user prompt is required, you do not need to specify a system prompt.

Both the `system` and `user` prompts are string values and can be defined in any way which YAML supports. The example above is using a multiline string where the carriage returns are preserved.

YAML has great support for multiline string values through [Block Scalars](https://yaml-multiline.info). With these it supports both _literal_ and _folded_ strings. With literal strings the new line characters in the input string are maintained and the string remains exactly as written. With folded the new line characters are collapsed and replaced by a space character, allowing you to write very long strings over multiple lines. Using folded, if you use two new line characters, then a newline is added to the string.

```yaml
# Folded
example: >
  The ships hung
  in the sky in much the same
  way as bricks don't

# Produces:
# The ships hung in the sky in much the same way as bricks don't
```

```yaml
# Literal
example: |
  The ships hung
  in the sky in much the same
  way as bricks don't

# Produces:
# The ships hung
# in the sky in much the same
# way as bricks don't
```

#### Prompt templates

The syntax of the prompts uses the [Fluid](https://github.com/sebastienros/fluid) templating language, which is itself based on [Liquid](https://shopify.github.io/liquid/) created by Shopify. This templating language allows us to define user prompts which can change depending on values being passed into the template parser.

In the example above you can see `{{ topic }}` which is a placeholder for the value being passed in, and will be substituted straight into the template. There is also the `{% if style -%} ... {% endif -%}` section which tells the parser to only include this section if the `style` parameter has a value. The `-%}` at the end of marker contains the hyphen symbol which tells the parser that it should collapse the blank lines.

There is a great tutorial on writing templates with Fluid available [online](https://deanebarker.net/tech/fluid/).

When you generate the prompt it does not replace the template, only giving you the generated output. This means you can generate the prompt as many times as you want with different input values.

### Few-shot prompting

`fewShots` is a section to allow the prompt writer to provide [few-shot prompting](https://www.promptingguide.ai/techniques/fewshot) techniques to the solution. When constructing a prompt you would include these, along with your system prompt, and then the user prompt, this provides examples on how the LLM should respond to the user prompt. If you're using OpenAI or Azure OpenAI then you can use the extension methods (see later) which will create all the messages for you.

## Examples

### Loading a prompt file directly

Prompt files can be accessed directly. If you have only a couple of files or want to quickly test them out then this is a fairly simple way of doing so.

```csharp
using DotPrompt;

var promptFile = PromptFile.FromFile("path/to/prompt-file.prompt");

var systemPrompt = promptFile.GetSystemPrompt(null);
var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
{
    { "topic", "bluetooth" },
    { "style", "used car salesman" }
});
```

If the prompt file contained the example above, then it would produce the following.

```text
System Prompt:
You are a helpful research assistant who will provide descriptive responses for a given topic and how it impacts society

User Prompt:
Explain the impact of bluetooth on how we engage with technology as a society
Can you answer in the style of a used car salesman
```

This might result in a response from the LLM which looks like this (sorry)

> Ladies and gentlemen, gather 'round and let me tell you about the miracle of modern technology that's revolutionized the way we connect with our gadgets—I'm talking about Bluetooth! Bluetooth is the unsung hero, the secret sauce that's been making our lives more convenient, more connected, and definitely more high-tech. Picture this: seamless, wire-free communication between your favorite devices. No more tangled cords, no more mess. It's like having a VIP pass to the front row of the future!
> 
> ...

### Using the Prompt Manager

The prompt manager is the preferred method for handling your prompt files. It allows you to load them from a location, access then by name, and then use them in your application.

The default for the prompt manager is to access files in the local `prompts` folder, though you can specify a different path if you want to.

```csharp
// Load from the default location of the `prompts` directory
var promptManager = new PromptManager();
var promptFile = promptManager.GetPromptFile("example");

// Use a different folder
var promptManager = new PromptManager("another-location");

var promptFile = promptManager.GetPromptFile("example");

// List all of the prompts loaded
var promptNames = promptManager.ListPromptFileNames();
```

The prompt manager implements an `IPromptManager` interface, and so if you want to use this through a DI container, or IoC pattern, then you can easily provide a mocked version for testing.

The prompt manager can also take an `IPromptStore` instance which allows you to build a custom store which might not be file-based (see [Creating a custom prompt store](#creating-a-custom-prompt-store)). This also allows for providing a mocked interface so you can write unit tests which are not dependent on the storage mechanism.

### Full examples

Using the prompt manager to read a prompt and then use it in a call to an Azure OpenAI endpoint.

_N.B._ This example assumes that there is a `prompts` directory with the prompt file available.

```csharp
using System.ClientModel;
using Azure.AI.OpenAI;
using DotPrompt;

var openAiClient = new(new Uri("https://endpoint"), new ApiKeyCredential("abc123"));

var promptManager = new PromptManager();
var promptFile = promptManager.GetPromptFile("example");

// The system prompt and user prompt methods take dictionaries containing the values needed for the
// template. If none are needed you can simply pass in null.
var systemPrompt = promptFile.GetSystemPrompt(null);
var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
{
    { "topic", "bluetooth" },
    { "style", "used car salesman" }
});

var client = openAiClient.GetChatClient(promptFile.Model ?? "default-model");

var completion = await client.CompleteChatAsync(
    [
        new SystemChatMessage(systemPrompt),
        new UserChatMessage(userPrompt)
    ],
    new ChatCompletionOptions
    (
        ResponseFormat = promptFile.OutputFormat == OutputFormat.Json ? ChatResponseFormat.JsonObject : ChatResponseFormat.Text,
        Temperature = promptFile.Config.Temperature,
        MaxTokens = promptFile.Config.MaxTokens
    )
);
```

Or, using the OpenAI provided extension methods.

```csharp
using System.ClientModel;
using Azure.AI.OpenAI;
using DotPrompt;
using DotPrompt.Extensions.OpenAi;

var openAiClient = new(new Uri("https://endpoint"), new ApiKeyCredential("abc123"));

var promptManager = new PromptManager();
var promptFile = promptManager.GetPromptFile("example");

var promptValues = new Dictionary<string, object>
{
    { "topic", "bluetooth" },
    { "style", "used car salesman" }
};

var client = openAiClient.GetChatClient(promptFile.Model ?? "default-model");

var completion = await client.CompleteChatAsync(
    promptFile.ToOpenAiChatMessages(promptValues),
    promptFile.ToOpenAiChatCompletionOptions()
);

var response = completion.Value;
Console.WriteLine(response.Content[0].Text);
```

And now, if we need to modify our prompt, we can simply change the prompt file and leave our code alone (assuming the parameters don't change).

## Creating a custom prompt store

The above shows how you can use DotPrompt to read prompt files from disk. But what if you have a situation where you want your prompts somewhere more central, like a cloud storage service, or a database? Well The prompt manager can take an `IPromptStore` instance as an argument. In all the examples above it's using the `FilePromptStore` which is included, but you can also build your own. It just needs to implement the interface and you're done.

To give you an example, here's a simple implementation which uses an Azure Storage Table Store to hold the prompt details.

```csharp
/// <summary>
/// Implementation of the IPromptStore for Azure Storage Tables
/// </summary>
public class AzureTablePromptStore : IPromptStore
{
    /// <summary>
    /// Loads the prompts from the table store
    /// </summary>
    public IEnumerable<PromptFile> Load()
    {
        var tableClient = GetTableClient();
        var promptEntities = tableClient.Query<PromptEntity>(e => e.PartitionKey == "DotPromptTest");

        var promptFiles = promptEntities
            .Select(pe => pe.ToPromptFile())
            .ToList();

        return promptFiles;
    }

    /// <summary>
    /// Gets a table client
    /// </summary>
    private static TableClient GetTableClient()
    {
        // Replace the configuration items here with your value or switch to using
        // Entra based authentication
        var client = new TableServiceClient(
            new Uri($"https://{Configuration.StorageAccountName}.table.core.windows.net/"),
            new TableSharedKeyCredential(Configuration.StorageAccountName, Configuration.StorageAccountKey)
        );

        var tableClient = client.GetTableClient("prompts");
        tableClient.CreateIfNotExists();

        return tableClient;
    }
}

/// <summary>
/// Represents a record held in the storage table
/// </summary>
public class PromptEntity : ITableEntity
{
    /// <summary>
    /// Gets, sets the partition key for the record
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the row key for the record
    /// </summary>
    public string RowKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the timestamp of the entry
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
    
    /// <summary>
    /// Gets, sets the records ETag value
    /// </summary>
    public ETag ETag { get; set; }
    
    /// <summary>
    /// Gets, sets the model to use
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Gets, sets the output format
    /// </summary>
    public string OutputFormat { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the maximum number of tokens
    /// </summary>
    public int MaxTokens { get; set; }
    
    /// <summary>
    /// Gets, sets the parameter information which is held as a JSON string value
    /// </summary>
    public string Parameters { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the default values which are held as a JSON string value
    /// </summary>
    public string Default { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the system prompt template
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets, sets the user prompt template
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Returns the prompt entity record into a <see cref="PromptFile"/> instance
    /// </summary>
    /// <returns></returns>
    public PromptFile ToPromptFile()
    {
        var parameters = new Dictionary<string, string>();
        var defaults = new Dictionary<string, object>();
        
        // If there are parameter values then convert them into a dictionary
        if (!string.IsNullOrEmpty(Parameters))
        {
            var entityParameters = (JsonObject)JsonNode.Parse(Parameters)!;
            foreach (var (prop, propType) in entityParameters)
            {
                parameters.Add(prop, propType?.AsValue().ToString() ?? string.Empty);
            }
        }

        // If there are default values then convert them into a dictionary
        if (!string.IsNullOrEmpty(Default))
        {
            var entityDefaults = (JsonObject)JsonNode.Parse(Default)!;
            foreach (var (prop, defaultValue) in entityDefaults)
            {
                defaults.Add(prop, defaultValue?.AsValue().GetValue<object>() ?? string.Empty);
            }
        }
        
        // Generate the new prompt file
        var promptFile = new PromptFile
        {
            Name = RowKey,
            Model = Model,
            Config = new PromptConfig
            {
                OutputFormat = Enum.Parse<OutputFormat>(OutputFormat, true),
                MaxTokens = MaxTokens,
                Input = new InputSchema
                {
                    Parameters = parameters,
                    Default = defaults
                }
            },
            Prompts = new Prompts
            {
                System = SystemPrompt,
                User = UserPrompt
            }
        };

        return promptFile;
    }
}
```

And then to use this we would do the following

```csharp
var promptManager = new PromptManager(new AzureTablePromptStore());
var promptFile = promptManager.GetPromptFile("example");
```

## Upcoming features

There's still scope for work to do here and some of the items we're looking at include

* Additional configuration options
* Additional prompting techniques
* Open to feedback. Is there anything you'd like to see? Let us know
