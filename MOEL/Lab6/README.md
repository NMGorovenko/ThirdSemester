# ЛР6 — Телеграм‑бот с генерацией текста (YandexGPT + шаблоны)

В этой работе реализован автономный диалоговый бот для Telegram на C#, который отвечает на вопросы по теме магистерской работы.

Бот поддерживает два режима:
- **YandexGPT** — ответы генерируются облачной языковой моделью YandexGPT.
- **Template** — ответы строятся по заранее заданным русским шаблонам (для сравнения с ЛР5 и демонстрации «шаблонного» подхода).

Публичный бот в Telegram: `@gorovenko_lab6_bot`.

Архитектура:
- .NET 8, `Generic Host`, DI, конфигурация через `IOptions<T>`.
- Telegram‑клиент через `Telegram.Bot`.
- Интеграция с YandexGPT через HTTP‑клиент (`HttpClient` + `System.Net.Http.Json`).

## Как работает бот и какие есть команды

Публичный бот в Telegram: `@gorovenko_lab6_bot`.

Основное взаимодействие:
- бот работает в режиме long polling, сообщения читаются и обрабатываются в `TelegramBotHostedService`;
- на каждое входящее текстовое сообщение выбирается генератор (`YandexGPT` или `template`) через `IChatGeneratorResolver`;
- для режима `YandexGPT` в системный промпт подмешиваются материалы из папки `Knowledge` (тема, цели, структура проекта и т.п.), чтобы ответы были привязаны к магистерской работе.

Доступные команды:
- `/start` — приветственное сообщение и краткая инструкция:
  - рассказывает, что бот поддерживает два режима;
  - подсказывает, что для смены режима есть команды `/mode_gpt` и `/mode_template`.
- `/mode_gpt` — включает режим YandexGPT:
  - все последующие сообщения в этом чате обрабатываются через `YandexGptChatGenerator`;
  - модель получает системный промпт из `Knowledge/BASE.md` и дополнительные файлы `KNOWLEDGE_*.md` в качестве контекста, поэтому отвечает именно в рамках магистерской работы.
- `/mode_template` — включает шаблонный режим:
  - ответы собираются по заранее заданным русским шаблонам;
  - на выходе — структурированные, но ограниченные по разнообразию фразы (baseline для сравнения с YandexGPT).

Любой другой текст:
- бот определяет текущий режим для чата;
- вызывает `GenerateAsync` у соответствующего генератора:
  - в режиме `gpt` — отправляет запрос в YandexGPT с системным промптом и контекстом по магистерской;
  - в режиме `template` — формирует ответ по шаблону (вопрос/позитив/нейтрально) и явно помечает, что это шаблонный ответ;
- отправляет сформированный текст в чат.

## Структура

- `Lab6/src/Lab6.TelegramBot.csproj` — проект бота.
- `Lab6/src/Program.cs` — точка входа, настройка `Host` и DI.
- `Lab6/src/TelegramBotHostedService.cs` — фоновый сервис, который слушает Telegram и роутит сообщения в генераторы.
- `Lab6/src/Options/TelegramOptions.cs` — настройки бота (токен, режим по умолчанию).
- `Lab6/src/Options/YandexGptOptions.cs` — настройки YandexGPT (ключ, endpoint, модель, системный промпт).
- `Lab6/src/Generation/IChatGenerator.cs` — интерфейс генератора текста.
- `Lab6/src/Generation/TemplateChatGenerator.cs` — генератор по шаблонам (режим `template`).
- `Lab6/src/Generation/YandexGptChatGenerator.cs` — генератор через YandexGPT (режим `gpt`).
- `Lab6/src/Generation/ChatGeneratorResolver.cs` — выбор генератора по режиму/настройкам.
- `Lab6/src/appsettings.json` — пример конфигурации (без секретов).
- `Lab6/Dockerfile` — образ для контейнеризации бота.

## Настройка Telegram‑бота

1. В Telegram напишите боту `@BotFather`:
   - команда `/newbot`;
   - задайте имя и username;
   - BotFather вернёт токен вида `1234567890:AA...`.
2. Сохраните токен, **не добавляйте его в git**.

В коде токен читается из конфигурации `Telegram:Token`. Рекомендуемый способ передачи — через переменную окружения:

- `TELEGRAM__TOKEN=1234567890:AA...`

(`__` в имени переменной заменяет двоеточие `:` в пути к секции, это стандарт для `Microsoft.Extensions.Configuration`.)

## Настройка YandexGPT

Ниже приведён общий план, точные шаги зависят от настроек аккаунта Yandex Cloud:

1. Создайте каталог (folder) в Yandex Cloud и включите в нём сервис YandexGPT / Foundation Models.
2. Создайте сервисный аккаунт и ключ доступа (API key / IAM‑токен) с правами на использование модели.
3. Определите:
   - **Endpoint** — базовый URL API (например, `https://llm.api.cloud.yandex.net/foundationModels/v1/`).
   - **Model** — идентификатор модели, например:  
     `gpt://<folder-id>/yandexgpt-lite`.
4. Передайте настройки через переменные окружения:
   - `YANDEXGPT__APIKEY=<ваш API‑ключ>`
   - `YANDEXGPT__ENDPOINT=https://llm.api.cloud.yandex.net/foundationModels/v1/`
   - `YANDEXGPT__MODEL=gpt://<folder-id>/yandexgpt-lite`
   - (опционально) `YANDEXGPT__SYSTEMPROMPT="Ты диалоговый помощник по теме магистерской работы пользователя..."`.

Если ключ/endpoint/модель не заданы, `YandexGptChatGenerator` вернёт информативное сообщение о том, что интеграция не настроена.

## Конфигурация (appsettings.json + переменные окружения)

Пример `Lab6/src/appsettings.json`:

```json
{
  "Telegram": {
    "Token": "",
    "DefaultMode": "gpt"
  },
  "YandexGpt": {
    "ApiKey": "",
    "Endpoint": "https://llm.api.cloud.yandex.net/foundationModels/v1/",
    "Model": "gpt://<folder-id>/yandexgpt-lite",
    "SystemPrompt": "Ты диалоговый помощник по теме магистерской работы пользователя. Отвечай по-русски, кратко и по делу."
  }
}
```

В репозитории хранится **только пример** (пустые токены). На боевом окружении значения переопределяются через переменные окружения:

- `TELEGRAM__TOKEN`
- `TELEGRAM__DEFAULTMODE` (`gpt` или `template`)
- `YANDEXGPT__APIKEY`
- `YANDEXGPT__ENDPOINT`
- `YANDEXGPT__MODEL`
- `YANDEXGPT__SYSTEMPROMPT`

Дополнительно используются среда и соответствующие файлы конфигурации:

- `DOTNET_ENVIRONMENT=Local` → `appsettings.Local.json`
- `DOTNET_ENVIRONMENT=Development` → `appsettings.Development.json`
- `DOTNET_ENVIRONMENT=Production` → `appsettings.Production.json`

Сначала загружается базовый `appsettings.json`, затем — файл для текущего окружения, потом переменные окружения (они имеют наивысший приоритет).

## Локальный запуск (без Docker)

Предполагается, что .NET SDK 8.0+ уже установлен.

```bash
cd MOEL/Lab6/src

# В Linux/macOS:
export TELEGRAM__TOKEN="1234567890:AA..."
export TELEGRAM__DEFAULTMODE="gpt"
export YANDEXGPT__APIKEY="yc1...."
export YANDEXGPT__ENDPOINT="https://llm.api.cloud.yandex.net/foundationModels/v1/"
export YANDEXGPT__MODEL="gpt://<folder-id>/yandexgpt-lite"

# Выбор окружения (Local/Development/Production)
export DOTNET_ENVIRONMENT="Local"

dotnet run --configuration Release
```

После запуска:
- бот подключится к Telegram и начнёт long polling;
- можно писать ему в чате:
  - `/start` — приветствие и короткая инструкция;
  - `/mode_gpt` — включить режим YandexGPT;
  - `/mode_template` — включить шаблонный режим;
  - любой другой текст — получить ответ выбранного генератора.

## Сборка Docker‑образа

Из корня репозитория:

```bash
cd MOEL
docker build -f Lab6/Dockerfile -t moel-lab6-bot .
```

Проверка образа локально:

```bash
docker run --rm \
  -e TELEGRAM__TOKEN="1234567890:AA..." \
  -e TELEGRAM__DEFAULTMODE="gpt" \
  -e YANDEXGPT__APIKEY="yc1...." \
  -e YANDEXGPT__ENDPOINT="https://llm.api.cloud.yandex.net/foundationModels/v1/" \
  -e YANDEXGPT__MODEL="gpt://<folder-id>/yandexgpt-lite" \
  moel-lab6-bot
```

Контейнер не открывает порты (бот работает по исходящим HTTP + Telegram), поэтому никаких `-p` пробросов не требуется.

## Публикация на выделенный сервер

Рассмотрим типичный случай: есть Linux‑сервер (Ubuntu) с доступом по SSH и установленным Docker.

1. Склонировать репозиторий на сервер:

   ```bash
   git clone https://github.com/NMGorovenko/ThirdSemester.git
   cd ThirdSemester/MOEL
   ```

2. Собрать образ (можно на сервере, а можно заранее и загрузить в Docker Registry):

   ```bash
   docker build -f Lab6/Dockerfile -t moel-lab6-bot .
   ```

3. Запустить контейнер в фоне с автоматическим рестартом:

   ```bash
   docker run -d \
     --name moel-lab6-bot \
     --restart unless-stopped \
     -e TELEGRAM__TOKEN="1234567890:AA..." \
     -e TELEGRAM__DEFAULTMODE="gpt" \
     -e YANDEXGPT__APIKEY="yc1...." \
     -e YANDEXGPT__ENDPOINT="https://llm.api.cloud.yandex.net/foundationModels/v1/completion" \
     -e YANDEXGPT__MODEL="gpt://<folder-id>/yandexgpt-lite" \
     moel-lab6-bot
   ```

4. Проверить логи:

   ```bash
   docker logs -f moel-lab6-bot
   ```

   В логах должны появиться сообщения о запуске бота и обработке входящих сообщений.

Альтернатива без Docker — запуск через `systemd`:
- создать unit‑файл `moel-lab6-bot.service`, который запускает `dotnet Lab6.TelegramBot.dll` из опубликованной директории;
- передать секреты через `Environment=` / `EnvironmentFile=`.

## Режимы генерации и сравнение

Бот поддерживает два режима работы, переключаемых командами:

- `/mode_gpt` — ответы строятся через `YandexGptChatGenerator`:
  - гибкая генерация, реальные языковые модели;
  - учитывает полный контекст сообщения;
  - удобно использовать для диалога по теме магистерской работы.

- `/mode_template` — ответы строятся через `TemplateChatGenerator`:
  - используется набор фиксированных русских шаблонов (благодарность, вопрос, нейтральное описание);
  - на выходе — осмысленный, но ограниченный по структуре текст;
  - хороший baseline для сравнения с YandexGPT (аналог «шаблонного генератора» из задания).

Для отчёта по ЛР6 при взаимодействии с ботом можно:
- привести скриншоты / текстовые фрагменты диалога в режиме YandexGPT;
- повторить те же запросы в режиме `template`;
- описать различия: разнообразие формулировок, качество ответов, умение YandexGPT поддерживать контекст и ссылаться на элементы магистерской работы.

Таким образом:
- реализован автономный диалоговый Telegram‑бот на C#;
- поддерживаются два подхода к генерации (шаблоны и языковая модель YandexGPT);
- секреты передаются только через переменные окружения (подходящая практика для GitHub Secrets и выделенного сервера);
- есть Docker‑образ и инструкция по развёртыванию на удалённой машине.
