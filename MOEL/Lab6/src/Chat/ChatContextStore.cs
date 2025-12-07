using System.Collections.Concurrent;

namespace Lab6.TelegramBot.Chat;

public interface IChatContextStore
{
    /// <summary>
    /// Добавить сообщение в историю чата.
    /// role: "user" | "assistant".
    /// </summary>
    void Append(long chatId, string role, string text);

    /// <summary>
    /// Получить историю переписки (последние сообщения) с учётом TTL.
    /// </summary>
    IReadOnlyList<ChatMessage> GetHistory(long chatId);

    /// <summary>
    /// Очистить историю конкретного чата.
    /// </summary>
    void Clear(long chatId);
}

public record ChatMessage(string Role, string Text, DateTimeOffset Timestamp);

/// <summary>
/// Простое in-memory хранилище истории переписки по chatId с TTL 30 минут.
/// </summary>
public class ChatContextStore : IChatContextStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private static readonly int MaxMessagesPerChat = 50;

    private readonly ConcurrentDictionary<long, List<ChatMessage>> _storage = new();

    public void Append(long chatId, string role, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var list = _storage.GetOrAdd(chatId, _ => new List<ChatMessage>());

        lock (list)
        {
            // удалить устаревшие сообщения
            var cutoff = now - Ttl;
            list.RemoveAll(m => m.Timestamp < cutoff);

            list.Add(new ChatMessage(role, text, now));

            // ограничение по длине истории
            if (list.Count > MaxMessagesPerChat)
            {
                var toRemove = list.Count - MaxMessagesPerChat;
                list.RemoveRange(0, toRemove);
            }
        }
    }

    public IReadOnlyList<ChatMessage> GetHistory(long chatId)
    {
        if (!_storage.TryGetValue(chatId, out var list) || list.Count == 0)
        {
            return Array.Empty<ChatMessage>();
        }

        var now = DateTimeOffset.UtcNow;
        var cutoff = now - Ttl;

        lock (list)
        {
            list.RemoveAll(m => m.Timestamp < cutoff);
            return list.ToList();
        }
    }

    public void Clear(long chatId)
    {
        _storage.TryRemove(chatId, out _);
    }
}

