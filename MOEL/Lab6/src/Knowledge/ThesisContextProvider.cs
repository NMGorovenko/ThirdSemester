using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab6.TelegramBot.Knowledge;

/// <summary>
/// Загружает и кэширует текстовый контекст о магистерской работе из файлов в папке Knowledge.
/// </summary>
public class ThesisContextProvider
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ThesisContextProvider> _logger;
    private string? _cachedContext;
    private string? _cachedBasePrompt;

    public ThesisContextProvider(IHostEnvironment environment, ILogger<ThesisContextProvider> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Базовый системный промпт из файла BASE.md (если он существует).
    /// </summary>
    public string? GetBasePrompt()
    {
        if (_cachedBasePrompt != null)
        {
            return _cachedBasePrompt;
        }

        try
        {
            var knowledgeDir = Path.Combine(_environment.ContentRootPath, "Knowledge");
            if (!Directory.Exists(knowledgeDir))
            {
                _cachedBasePrompt = string.Empty;
                return _cachedBasePrompt;
            }

            var basePath = Path.Combine(knowledgeDir, "BASE.md");
            if (!File.Exists(basePath))
            {
                _cachedBasePrompt = string.Empty;
                return _cachedBasePrompt;
            }

            _cachedBasePrompt = File.ReadAllText(basePath);
            return _cachedBasePrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load BASE.md from Knowledge directory");
            _cachedBasePrompt = string.Empty;
            return _cachedBasePrompt;
        }
    }

    public string GetContext()
    {
        if (_cachedContext != null)
        {
            return _cachedContext;
        }

        try
        {
            var knowledgeDir = Path.Combine(_environment.ContentRootPath, "Knowledge");

            if (!Directory.Exists(knowledgeDir))
            {
                _logger.LogWarning("Knowledge directory not found at {Path}", knowledgeDir);
                _cachedContext = string.Empty;
                return _cachedContext;
            }

            var files = Directory.GetFiles(knowledgeDir, "*.md", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(Path.GetFileName(path), "BASE.md", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
            {
                _logger.LogWarning("No *.md files found in Knowledge directory at {Path}", knowledgeDir);
                _cachedContext = string.Empty;
                return _cachedContext;
            }

            var sb = new StringBuilder();
            foreach (var file in files)
            {
                sb.AppendLine($"# {Path.GetFileNameWithoutExtension(file)}");
                sb.AppendLine(File.ReadAllText(file));
                sb.AppendLine();
            }

            _cachedContext = sb.ToString();
            return _cachedContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load thesis context from Knowledge directory");
            _cachedContext = string.Empty;
            return _cachedContext;
        }
    }
}
