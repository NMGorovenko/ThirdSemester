using Lab6.TelegramBot.Generation;
using Lab6.TelegramBot.Chat;
using Lab6.TelegramBot.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Lab6.TelegramBot;

/// <summary>
/// Background service that connects to Telegram and processes updates.
/// </summary>
public class TelegramBotHostedService : BackgroundService
{
    private readonly ILogger<TelegramBotHostedService> _logger;
    private readonly TelegramOptions _telegramOptions;
    private readonly IChatGeneratorResolver _resolver;
    private readonly IChatContextStore _chatContextStore;
    private TelegramBotClient? _botClient;

    public TelegramBotHostedService(
        ILogger<TelegramBotHostedService> logger,
        IOptions<TelegramOptions> telegramOptions,
        IChatGeneratorResolver resolver,
        IChatContextStore chatContextStore)
    {
        _logger = logger;
        _telegramOptions = telegramOptions.Value;
        _resolver = resolver;
        _chatContextStore = chatContextStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_telegramOptions.Token))
        {
            _logger.LogError("Telegram Bot token is not configured. Set Telegram:Token (or TELEGRAM__TOKEN env).");
            return;
        }

        _botClient = new TelegramBotClient(_telegramOptions.Token);

        // Запускаем приём обновлений через Telegram.Bot.TelegramBotClientExtensions.StartReceiving
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        Telegram.Bot.TelegramBotClientExtensions.StartReceiving(
            _botClient,
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken);

        _logger.LogInformation("Telegram bot receiving updates...");

        // BackgroundService должен жить, пока не будет отменён.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is null)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();
        var command = GetCommand(text);

        _logger.LogInformation("Received message from chat {ChatId}: {Text}", chatId, text);

        if (command is "/start" or "/help")
        {
            await botClient.SendMessage(
                chatId,
                BuildHelpMessage(chatId),
                cancellationToken: cancellationToken);
            return;
        }

        if (command == "/mode_gpt")
        {
            await botClient.SendMessage(
                chatId,
                "Режим YandexGPT активирован. Все последующие ответы будут строиться с использованием языковой модели (если она настроена).",
                cancellationToken: cancellationToken);
            _chatModes[chatId] = "gpt";
            return;
        }

        if (command == "/mode_template")
        {
            await botClient.SendMessage(
                chatId,
                "Режим шаблонного генератора активирован. Ответы будут строиться по заранее заданным шаблонам без языковой модели.",
                cancellationToken: cancellationToken);
            _chatModes[chatId] = "template";
            return;
        }

        // Определяем режим для данного чата.
        _chatModes.TryGetValue(chatId, out var mode);
        var generator = _resolver.Resolve(mode);

        // сначала генерируем ответ, затем добавляем в историю пару (user, assistant),
        // чтобы текущий вопрос не дублировался в history при формировании промпта
        var reply = await generator.GenerateAsync(chatId, text, cancellationToken);

        _chatContextStore.Append(chatId, "user", text);
        _chatContextStore.Append(chatId, "assistant", reply);

        await botClient.SendMessage(chatId, reply, cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError("Error in Telegram polling: {Error}", exception.Message);
        return Task.CompletedTask;
    }

    // simple in-memory per-chat mode store
    private readonly Dictionary<long, string> _chatModes = new();

    private string BuildHelpMessage(long chatId)
    {
        var currentMode = _chatModes.TryGetValue(chatId, out var mode) && !string.IsNullOrWhiteSpace(mode)
            ? mode
            : _telegramOptions.DefaultMode;

        return
            "Привет! Я бот для 6‑й лабораторной по МОЭЛ.\n" +
            "Я отвечаю на вопросы по теме магистерской работы двумя способами:\n" +
            "1) через YandexGPT (режим /mode_gpt)\n" +
            "2) через генерацию по шаблонам (режим /mode_template)\n\n" +
            $"Текущий режим для этого чата: {currentMode}\n\n" +
            "Команды:\n" +
            "/help — справка\n" +
            "/mode_gpt — включить YandexGPT\n" +
            "/mode_template — включить шаблонный режим\n\n" +
            "Просто напишите вопрос, а я попробую ответить.";
    }

    private static string? GetCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Telegram может присылать команды в формате "/start@botname" и/или с аргументами.
        var firstToken = text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var withoutMention = firstToken.Split('@', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        return withoutMention.ToLowerInvariant();
    }
}
