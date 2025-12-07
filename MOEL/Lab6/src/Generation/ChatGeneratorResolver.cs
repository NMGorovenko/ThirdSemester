using Lab6.TelegramBot.Options;
using Microsoft.Extensions.Options;

namespace Lab6.TelegramBot.Generation;

public interface IChatGeneratorResolver
{
    IChatGenerator Resolve(string? mode);
}

public class ChatGeneratorResolver : IChatGeneratorResolver
{
    private readonly IReadOnlyDictionary<string, IChatGenerator> _generators;
    private readonly string _defaultMode;

    public ChatGeneratorResolver(IEnumerable<IChatGenerator> generators, IOptions<TelegramOptions> telegramOptions)
    {
        _generators = generators.ToDictionary(x => x.Mode, StringComparer.OrdinalIgnoreCase);
        _defaultMode = string.IsNullOrWhiteSpace(telegramOptions.Value.DefaultMode)
            ? "gpt"
            : telegramOptions.Value.DefaultMode;
    }

    public IChatGenerator Resolve(string? mode)
    {
        if (!string.IsNullOrWhiteSpace(mode) && _generators.TryGetValue(mode, out var byMode))
        {
            return byMode;
        }

        if (_generators.TryGetValue(_defaultMode, out var byDefault))
        {
            return byDefault;
        }

        // Fallback: берём любой зарегистрированный генератор
        return _generators.Values.First();
    }
}

