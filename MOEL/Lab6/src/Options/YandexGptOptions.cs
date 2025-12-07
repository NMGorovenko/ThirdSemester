namespace Lab6.TelegramBot.Options;

public class YandexGptOptions
{
    /// <summary>
    /// OAuth/API key or IAM token for YandexGPT.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Endpoint base URL, e.g. "https://llm.api.cloud.yandex.net".
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Model name / deployment id.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional system prompt describing the bot.
    /// </summary>
    public string? SystemPrompt { get; set; }
}

