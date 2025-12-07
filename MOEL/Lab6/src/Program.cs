using System.Net.Http.Headers;
using Lab6.TelegramBot;
using Lab6.TelegramBot.Generation;
using Lab6.TelegramBot.Chat;
using Lab6.TelegramBot.Knowledge;
using Lab6.TelegramBot.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configuration: appsettings.json + appsettings.{Environment}.json + environment variables
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<YandexGptOptions>(builder.Configuration.GetSection("YandexGpt"));

builder.Services.AddSingleton<ThesisContextProvider>();
builder.Services.AddSingleton<IChatContextStore, ChatContextStore>();

// YandexGPT generator (typed HttpClient)
builder.Services.AddHttpClient<IChatGenerator, YandexGptChatGenerator>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<YandexGptOptions>>().Value;

    var baseUrl = string.IsNullOrWhiteSpace(opts.Endpoint)
        ? "https://llm.api.cloud.yandex.net/foundationModels/v1/"
        : opts.Endpoint!;

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
    {
        // Используем IAM/OAuth‑токен в виде Bearer, как в InterviewHelperBot
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
    }
});

// Template generator
builder.Services.AddSingleton<IChatGenerator, TemplateChatGenerator>();
builder.Services.AddSingleton<IChatGeneratorResolver, ChatGeneratorResolver>();

builder.Services.AddHostedService<TelegramBotHostedService>();

var host = builder.Build();

await host.RunAsync();
