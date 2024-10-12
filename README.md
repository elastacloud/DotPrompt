# DotPrompt

![NuGet Version](https://img.shields.io/nuget/v/DotPrompt)
![GitHub License](https://img.shields.io/github/license/elastacloud/DotPrompt)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/elastacloud/DotPrompt/dotnet.yml)


A simple library which allows you to build prompts using a configuration-based syntax without needing to embed them into your application. It supports templating for user prompts through the [Fluid](https://github.com/sebastienros/fluid) templating language, meaning you can re-use the same prompt and pass in different values at runtime. The templating language also allows for conditions, so you can change how the prompt is generated based on those variable values.

A prompt file is simply any file ending with a `.prompt` extension. The actual file itself is
simply a YAML configuration file, the extension allows us to quickly identify the file for its intended purpose.

## Note for JetBrains IDE users

There is a [known issue](https://youtrack.jetbrains.com/issue/IJPL-162378) with `.prompt` files causing unusual behaviour in tools such as Rider and IntelliJ. You can work around this by either disabling the Terminal plug-in or by using a different editor to modify the files.

## The Prompt File

A prompt file contents contain some top-level identification properties, followed by configuration information and then finally the prompts. A complete prompt file should look like this.

```yaml
name: Example
config:
  outputFormat: text
  temperature: 0.9
  maxOutputTokens: 500
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

The `name` is optional in the configuration, if it's not provided then the name is taken from the file name minus the extension. So a file called `gen-lookup-code.prompt` would get the name `gen-lookup-code`. This doesn't play a role in the generation of the prompts themselves (though future updates might), but allows you to identify the prompt source when logging.

The `config` section has some top level items which are provided for the client to use in their LLM calls to set options on each call. The `outputFormat` property takes a value of either `text` or `json` depending on how the LLM is intended to respond to the request. If specifying `json` then some LLMs require either the system or user prompt to state that the expected output is JSON as well. If the library does not detect the term `JSON` in the prompt then it will append a small statement to the system prompt requesting for the response to be in JSON format.

The `input` section is related to the `prompts` section, so I will cover them both at the same time.

The syntax of the prompts uses the [Fluid](https://github.com/sebastienros/fluid) templating language, which is itself based on [Liquid](https://shopify.github.io/liquid/) created by Shopify. This templating language allows us to define user prompts which can change depending on values being passed into the template parser.

In the example above you can see `{{ topic }}` which is a placeholder for the value being passed in, and will be substituted straight into the template. There is also `{% if style -%} ... {% endif -%}`  which tells the parser to only include this section if the `style` parameter has a value. The `-%}` at the end of marker contains the hyphen symbol which denotes that the parser should collapse the blank lines.

There is a great tutorial on writing templates with Fluid available [online](https://deanebarker.net/tech/fluid/).

Fluid allows for complex objects and custom parsers to be added to it, but for this package it applies only a subset, allowing strings, numbers, and booleans to be supplied as parameters. This is where the `input` section comes in.

The `input` section contains two subsections. `parameters` defines the parameters the template expects and what their value types are. If a parameter is denoted with a `?` at the end then it is considered an optional parameter, if the user does not provide a value, then this is allowed. The `default` subsection under `input` allows the prompt writer to specify default values to be used if values are not provided by the user.

If a user does not provide a value for a non-optional parameter, and a default does not exist, then this is considered an error and is reported as such.

`fewShots` is a method to allow the prompt writer to provide [few-shot prompting](https://www.promptingguide.ai/techniques/fewshot) techniques to the solution. When constructing a prompt you would include these, along with your system prompt, and then the user prompt, this provides examples on how the LLM should respond to the user prompt. If you're using OpenAI or Azure OpenAI then you can use the extension methods (see later) which will create all the messages for you.

### Tips for multi-line string values in YAML

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

The above-referenced article provides a good explanation of how to work with multiline strings, including the use of _chomp indicators_ which define how newline characters should be handled.

## Example

The overall solution can then be used in this way.

```csharp
using DotPrompt;

var promptFile = PromptFile.FromFile("path/to/prompt-file.prompt");

var systemPrompt = promptFile.GetSystemPrompt();
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
> Now, imagine strolling through the park, without a care in the world, your favorite tunes playing crystal clear in your ears from those sleek wireless earbuds. That, my friends, is Bluetooth working its magic! Hands-free calls in your car? Check. Syncing your smartphone to your smartwatch? Double check. Connecting all your smart home gadgets for the perfect, automated living experience? Bluetooth's got your back!
> 
> But wait, there's more! Bluetooth isn't just for the tech-savvy; it's a game-changer for everyone. Ever tried setting up a complex home audio system or those fancy fitness trackers? Thanks to Bluetooth, it's as easy as pie. With its user-friendly setup, you're ready to go quicker than you can say "wireless wonder."
> 
> Now, let's talk about reliability and security—Bluetooth's got it all. Fast, stable connections and advanced encryption mean you can trust your data is safe and sound. Perfect for business, education, and everyday life, Bluetooth is the versatile superstar of wireless communication.
> 
> So, folks, don't just stand there, embrace the wireless revolution! With Bluetooth, you're not just keeping up with technology—you're leading the charge. Step into a world of endless possibilities and experience firsthand how this small but mighty technology is making our lives more convenient, more connected, and absolutely fabulous. Be a part of the future, today!`

Parsing a prompt does not produce a finalised version of the parsed template, it can be produced multiple times with different values each time. All together this would look something like below.

```csharp
using System.ClientModel;
using Azure.AI.OpenAI;
using DotPrompt;

var openAiClient = new(new Uri("https://endpoint"), new ApiKeyCredential("abc123"));
var client = openAiClient.GetChatClient("model");

var promptFile = PromptFile.FromFile("path/to/prompt-file.prompt");

var systemPrompt = promptFile.GetSystemPrompt();
var userPrompt = promptFile.GetUserPrompt(new Dictionary<string, object>
{
    { "topic", "bluetooth" },
    { "style", "used car salesman" }
});

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
var client = openAiClient.GetChatClient("model");

var promptFile = PromptFile.FromFile("path/to/prompt-file.prompt");

var promptValues = new Dictionary<string, object>
{
    { "topic", "bluetooth" },
    { "style", "used car salesman" }
};

var completion = await client.CompleteChatAsync(
    promptFile.ToOpenAiChatMessages(promptValues),
    promptFile.ToOpenAiChatCompletionOptions()
);

var response = completion.Value;
Console.WriteLine(response.Content[0].Text);
```

And now, if we need to modify our prompt, we can simply change the prompt file and leave our code alone (assuming the parameters don't change).

## Upcoming features

There's still scope for work to do here and some of the items we're looking at include

* Additional configuration options
* Different prompt stores (and providing an interface so you can define your own)
* Additional prompting techniques
* A prompt manager, allowing you to define a single location for your prompts and then using the manager to access the right prompt
* Open to feedback. Is there something you'd like to see? Let us know

![Static Badge](https://img.shields.io/badge/Badges_by-shields.io-blue?link=https%3A%2F%2Fshields.io)
