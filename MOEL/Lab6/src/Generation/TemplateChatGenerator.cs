using System.Text;

namespace Lab6.TelegramBot.Generation;

/// <summary>
/// Simple template-based generator used for comparison with YandexGPT.
/// </summary>
public class TemplateChatGenerator : IChatGenerator
{
    private static readonly string[] PositiveTemplates =
    {
        "Спасибо за вопрос! Если кратко: {0}",
        "Интересное замечание. В контексте магистерской работы это можно описать так: {0}",
        "Хорошее направление мысли: {0}"
    };

    private static readonly string[] NeutralTemplates =
    {
        "Зафиксировал: {0}. Могу подробно рассказать, как это связано с магистерским исследованием.",
        "Вы пишете: {0}. Давайте разберём это по шагам.",
        "Я понял ваш запрос («{0}»). Сейчас объясню в терминах темы магистерской."
    };

    private static readonly string[] QuestionTemplates =
    {
        "Вы спрашиваете: {0}?\nКраткий ответ: для этого в магистерской работе используется предложенный подход к анализу текстов.",
        "Хороший вопрос: {0}? В рамках исследования это связано с моделью генерации ответов по семантическим признакам.",
        "Если переформулировать ваш вопрос («{0}»), он сводится к тем же проблемам, что и в магистерской задаче."
    };

    private readonly Random _random = new();

    public string Mode => "template";

    public Task<string> GenerateAsync(long chatId, string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult("Напишите, пожалуйста, вопрос по теме магистерской работы — я отвечу в шаблонном стиле.");
        }

        var trimmed = userMessage.Trim();
        var templates = ChooseTemplates(trimmed);
        var template = templates[_random.Next(templates.Length)];

        var builder = new StringBuilder();
        builder.AppendFormat(template, trimmed);
        builder.AppendLine();
        builder.AppendLine();
        builder.Append("Обратите внимание: этот ответ построен по заранее заданному шаблону, ");
        builder.Append("без полноценной языковой модели — он используется только для сравнения с YandexGPT.");

        return Task.FromResult(builder.ToString());
    }

    private static string[] ChooseTemplates(string text)
    {
        var lower = text.ToLowerInvariant();

        if (lower.Contains("?"))
        {
            return QuestionTemplates;
        }

        if (lower.Contains("спасибо") || lower.Contains("круто") || lower.Contains("отлично") || lower.Contains("нрав"))
        {
            return PositiveTemplates;
        }

        return NeutralTemplates;
    }
}
