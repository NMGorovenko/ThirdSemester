# CI/CD и секреты для ЛР6 (Telegram‑бот)

Этот файл описывает, какие GitHub Secrets нужны для публикации Docker‑образа бота и его деплоя на VPS, а также как в целом работает процесс релиза.

## Обзор пайплайна

Используются два workflow‑файла (см. `.github/workflows` в корне репозитория):

- `docker-publish.yml` — собирает Docker‑образ бота из `MOEL/Lab6` и публикует его в Docker Hub.
- `deploy-to-vps.yml` — разворачивает выбранную версию образа на выделенном VPS (через SSH и Docker).

Типичный сценарий:

1. Делаем коммит с изменениями в ЛР6 (код бота, Dockerfile и т.д.).
2. Создаём git‑тег (например, `lab6-v1.0.0`) и пушим его:
   - `git tag lab6-v1.0.0`
   - `git push origin lab6-v1.0.0`
3. Срабатывает workflow `docker-publish.yml`:
   - собирает образ из `MOEL/Lab6/Dockerfile`;
   - пушит в Docker Hub:
     - `${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:lab6-v1.0.0`
     - `${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:latest`.
4. Вручную запускаем `deploy-to-vps.yml` (через вкладку Actions):
   - либо без указания версии (если запуск из тега);
   - либо с явным `version`, например `lab6-v1.0.0`.
5. Workflow логинится на VPS, делает `docker pull`, останавливает старый контейнер и стартует новый `moel-gorovenko-lab6` с нужными переменными окружения (токен Telegram, ключ YandexGPT и т.п.).

## Набор GitHub Secrets

Все секреты задаются на уровне репозитория в GitHub:  
`Settings → Secrets and variables → Actions → New repository secret`.

### 1. Секреты для Docker Hub

Используются в `docker-publish.yml` и `deploy-to-vps.yml`.

- `DOCKERHUB_USERNAME`
  - **Что это:** логин вашего аккаунта Docker Hub.
  - **Где используется:** для логина в Docker Hub и формирования имени образа:
    - `${{ secrets.DOCKERHUB_USERNAME }}/moel-gorovenko-lab6:...`
  - **Зачем нужен:** чтобы пушить и потом тянуть образ бота.

- `DOCKERHUB_TOKEN`
  - **Что это:** персональный access token Docker Hub (не обычный пароль).
  - **Где используется:** в шагах `docker/login-action` и в SSH‑скрипте на VPS.
  - **Зачем нужен:** безопасная аутентификация при `docker login`.

### 2. Секреты доступа к VPS

Используются в `deploy-to-vps.yml`.

- `HOSTING_IP`
  - **Что это:** IP‑адрес или DNS‑имя VPS (например, `203.0.113.10`).
  - **Где используется:** в командах `ssh` / `sshpass`.
  - **Зачем нужен:** чтобы GitHub Actions знал, куда подключаться.

- `HOSTING_USER`
  - **Что это:** Linux‑пользователь на VPS (например, `deploy` или `root`).
  - **Где используется:** в `ssh` (`HOSTING_USER@HOSTING_IP`).
  - **Зачем нужен:** под каким пользователем запускать Docker на сервере.

- `HOSTING_ROOT_PASSWORD`
  - **Что это:** пароль пользователя `HOSTING_USER` (в исходном шаблоне назывался “root password”, можно использовать аккаунт с sudo/доступом к Docker).
  - **Где используется:** в `sshpass` для неинтерактивного входа.
  - **Зачем нужен:** авторизация по паролю; более безопасный вариант — перейти на SSH‑ключи, но текущий workflow рассчитан на пароль.

### 3. Секреты для Telegram‑бота

Используются в `deploy-to-vps.yml`.

- `TELEGRAM_BOT_TOKEN`
  - **Что это:** API‑токен, полученный у `@BotFather` при создании бота.
  - **Где используется:** в Docker‑контейнере как переменная окружения:
    - `TELEGRAM__TOKEN="${{ secrets.TELEGRAM_BOT_TOKEN }}"`
  - **Зачем нужен:** бот не может подключиться к Telegram API без токена.

### 4. Секреты для YandexGPT

Используются в `deploy-to-vps.yml`.

- `YANDEXGPT_APIKEY`
  - **Что это:** API‑ключ / IAM‑токен сервисного аккаунта в Yandex Cloud.
  - **Где используется:** передаётся в контейнер как `YANDEXGPT__APIKEY`.
  - **Зачем нужен:** авторизация запросов к YandexGPT.

- `YANDEXGPT_ENDPOINT`
  - **Что это:** URL endpoint’а YandexGPT, например  
    `https://llm.api.cloud.yandex.net/foundationModels/v1/completion`.
  - **Где используется:** как `YANDEXGPT__ENDPOINT`.
  - **Зачем нужен:** чтобы `YandexGptChatGenerator` знал, куда отправлять HTTP‑запросы.

- `YANDEXGPT_MODEL`
  - **Что это:** идентификатор модели, например  
    `gpt://<folder-id>/yandexgpt-lite`.
  - **Где используется:** как `YANDEXGPT__MODEL`.
  - **Зачем нужен:** выбор конкретной языковой модели при обращении к API.

(*Опционально можно добавить секрет* `YANDEXGPT_SYSTEMPROMPT`, если хотите держать системный промпт отдельно от репозитория и не в `appsettings.*.json`; тогда его можно прокинуть как `YANDEXGPT__SYSTEMPROMPT` в Docker run.)

## Как работает `docker-publish.yml`

Файл: `.github/workflows/docker-publish.yml`

- **Триггер:** `on: push: tags: ['*']`
  - Любой пуш тега (например, `lab6-v1.0.0`) запускает сборку.
- **Основные шаги:**
  1. **Checkout кода** — `actions/checkout@v4`.
  2. **Извлечение версии из тега:**
     - Берётся `GITHUB_REF`, отрезается префикс `refs/tags/`.
     - Переменная `VERSION` (например, `lab6-v1.0.0`) кладётся в `steps.get_version.outputs.VERSION`.
  3. **Логин в Docker Hub:**
     - `username: ${{ secrets.DOCKERHUB_USERNAME }}`
     - `password: ${{ secrets.DOCKERHUB_TOKEN }}`
  4. **Сборка и пуш образа:**
     - `context: ./MOEL`  
       (Dockerfile ожидает пути `Lab6/...` внутри этого контекста).
     - `file: ./MOEL/Lab6/Dockerfile`
     - `push: true`
     - `tags`:
       - `${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:${VERSION}`
       - `${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:latest`

Итог: после успешного прохода workflow в Docker Hub доступны как конкретная версия, так и тег `latest` бота.

## Как работает `deploy-to-vps.yml`

Файл: `.github/workflows/deploy-to-vps.yml`

- **Триггер:** `workflow_dispatch` (ручной запуск через GitHub Actions UI).
  - Можно указать `version` вручную;
  - если workflow запущен из контекста тега, берётся версия из `GITHUB_REF`.

- **Основные шаги:**

1. **Checkout кода.**
2. **Установка `sshpass`.**
3. **Определение версии:** шаг `get_version`:
   - если `GITHUB_REF` — тег (`refs/tags/*`), версия берётся оттуда;
   - иначе, если введён input `version`, берётся он;
   - иначе workflow падает с ошибкой — нужно указать версию.
4. **Деплой контейнера на VPS:**
   - Формируется имя образа:
     - `IMAGE="${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:${version}"`.
   - По SSH выполняются команды:
     1. `docker login` в Docker Hub через `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN`.
     2. `docker pull` нужной версии образа.
     3. Остановка и удаление старого контейнера:
        - `docker stop moel-gorovenko-lab6 || true`
        - `docker rm moel-gorovenko-lab6 || true`
     4. Запуск нового контейнера:
        - имя: `moel-gorovenko-lab6`
        - перезапуск: `--restart unless-stopped`
        - окружение:
          - `DOTNET_ENVIRONMENT=Production`
          - `TELEGRAM__TOKEN=${TELEGRAM_BOT_TOKEN}`
          - `TELEGRAM__DEFAULTMODE=gpt`
          - `YANDEXGPT__APIKEY=${YANDEXGPT_APIKEY}`
          - `YANDEXGPT__ENDPOINT=${YANDEXGPT_ENDPOINT}`
          - `YANDEXGPT__MODEL=${YANDEXGPT_MODEL}`
        - образ: `${DOCKERHUB_USERNAME}/moel-gorovenko-lab6:${version}`

## Как релизить бота “от начала до конца”

1. **Подготовить код и Dockerfile:**
   - убедиться, что `Lab6/src` собирается (`dotnet build`);
   - что `Lab6/Dockerfile` успешно собирается локально.

2. **Создать и запушить тег:**

```bash
git tag lab6-v1.0.0
git push origin lab6-v1.0.0
```

3. **Дождаться завершения `Build and Push Docker Image (Lab6 bot)`**  
   в GitHub Actions:
   - убедиться, что образ появился в Docker Hub.

4. **Запустить `Deploy Lab6 bot to VPS` вручную:**
   - зайти во вкладку Actions;
   - выбрать workflow `Deploy Lab6 bot to VPS`;
   - нажать `Run workflow`:
     - оставить `version` пустым (если запуск из тега);
     - или задать `lab6-v1.0.0` явно.

5. **Проверить логи на VPS:**

```bash
ssh HOSTING_USER@HOSTING_IP
docker logs -f moel-gorovenko-lab6
```

6. **Протестировать бота в Telegram:**
   - написать `/start`, `/help`, `/mode_gpt`, `/mode_template` и несколько текстов;
   - убедиться, что ответы приходят и соответствуют выбранному режиму.

Такой процесс закрывает требования ЛР6:
- бот развёрнут в открытом репозитории,
- есть автоматизированная сборка Docker‑образа,
- есть сценарий выката на выделенный сервер,
- конфиденциальные данные (токены и пароли) хранятся в GitHub Secrets и передаются в контейнер только через переменные окружения.
