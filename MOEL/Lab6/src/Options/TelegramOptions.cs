namespace Lab6.TelegramBot.Options;

public class TelegramOptions
{
    /// <summary>
    /// Telegram Bot API token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Default mode: "gpt" or "template".
    /// </summary>
    public string DefaultMode { get; set; } = "gpt";
}

