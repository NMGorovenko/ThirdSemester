using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lab6.TelegramBot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lab6.TelegramBot.Generation;

/// <summary>
/// Generator that calls YandexGPT via HTTP API.
/// </summary>
public class YandexGptChatGenerator : IChatGenerator
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<YandexGptOptions> _options;
    private readonly ILogger<YandexGptChatGenerator> _logger;

    public YandexGptChatGenerator(HttpClient httpClient, IOptions<YandexGptOptions> options, ILogger<YandexGptChatGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public string Mode => "gpt";

    public async Task<string> GenerateAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey) || string.IsNullOrWhiteSpace(opts.Model))
        {
            _logger.LogWarning("YandexGPT is not configured (missing ApiKey or Model). Falling back to a stub response.");
            return "YandexGPT пока не настроен (нет токена или modelUri). Проверьте переменные окружения для интеграции.";
        }

        try
        {
            var systemPrompt = string.IsNullOrWhiteSpace(opts.SystemPrompt)
                ? "Ты диалоговый помощник, который отвечает на вопросы по теме магистерской работы пользователя. Пиши по-русски, кратко и по делу."
                : opts.SystemPrompt;

            var requestBody = new
            {
                modelUri = opts.Model,
                completionOptions = new
                {
                    stream = false,
                    temperature = 0.4,
                    maxTokens = 400
                },
                messages = new[]
                {
                    new { role = "system", text = systemPrompt },
                    new { role = "user", text = userMessage }
                }
            };

            // HttpClient уже сконфигурирован в Program.cs (BaseAddress + Bearer Auth),
            // здесь отправляем только относительный путь "completion".
            using var request = new HttpRequestMessage(HttpMethod.Post, "completion")
            {
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            // Пробуем вытащить текст модели из типичных полей; если структура другая — будет лог и fallback.
            if (doc.RootElement.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("alternatives", out var alts) &&
                alts.ValueKind == JsonValueKind.Array &&
                alts.GetArrayLength() > 0)
            {
                var alt = alts[0];
                if (alt.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "(пустой ответ от YandexGPT)";
                }
            }

            _logger.LogWarning("Unexpected YandexGPT response format: {Json}", doc.RootElement.ToString());
            return "Не удалось разобрать ответ YandexGPT (неожиданный формат JSON).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling YandexGPT");
            return "При обращении к YandexGPT произошла ошибка. Попробуйте ещё раз позже.";
        }
    }
}
