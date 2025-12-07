namespace Lab6.TelegramBot.Generation;

/// <summary>
/// Abstraction over different text generators.
/// </summary>
public interface IChatGenerator
{
    /// <summary>
    /// Generator identifier, e.g. "gpt" or "template".
    /// </summary>
    string Mode { get; }

    /// <summary>
    /// Generate a reply for a user message.
    /// </summary>
    Task<string> GenerateAsync(string userMessage, CancellationToken cancellationToken = default);
}

