namespace DotPrompt;

/// <summary>
/// Represents errors that can occur when handling .prompt files
/// </summary>
public class DotPromptException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotPromptException"/> class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DotPromptException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DotPromptException"/> class with a specified error message and a
    /// reference to the inner exception that is the cause of this exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DotPromptException(string message, Exception innerException) : base(message, innerException) { }
}