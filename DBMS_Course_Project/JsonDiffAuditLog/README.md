# Журнал аудита с JSON‑diff и «снимками» (C‑расширение + .NET)

Прозрачная история изменений таблиц PostgreSQL: сохраняем before/after JSON, считаем компактный JSON‑diff на C‑расширении и восстанавливаем снимки записи на выбранный момент времени. В комплекте: Docker (Postgres + C‑расширение), SQL‑схема и триггеры, .NET‑демо c Dapper и интеграционные тесты.

## Цели

- Авто‑аудит UPDATE/DELETE (и INSERT): сохранять `before`/`after`, считать JSON‑diff на C‑расширении.
- Формировать компактную, человекочитаемую историю и быстро собирать снимки записи.
- Доставить воспроизводимое демо на Docker Compose и .NET.

## Компоненты

- Расширение PostgreSQL: собственное C‑расширение `jsondiff` (PGXS), вычисляющее JSON‑diff.
- Схема: `audit_log(table_name, pk, ts, op, before jsonb, after jsonb, diff jsonb)`.
- Триггеры: AFTER INSERT/UPDATE/DELETE — берут JSON из столбца `data` и считают diff на C‑функции.
- Функции:
  - `compute_json_diff(before jsonb, after jsonb) -> jsonb` (C, через SPI, поверхностный diff)
  - `snapshot_after_at(table_name text, pk jsonb, ts timestamptz) -> jsonb`
  - `snapshot_from_diffs_at(table_name text, pk jsonb, ts timestamptz) -> jsonb`
- Демоприложение: .NET console + Dapper, показывает 3 обновления, диффы и сборку снимка.
- Тесты: интеграционные, проверяют аудит и сборку снимков.

## Быстрый старт

Требуется: Docker, Docker Compose, .NET 8 SDK.

1) Запустить Postgres с C‑расширением `jsondiff` и инициализировать схему

```
cd DBMS_Course_Project/JsonDiffAuditLog
docker compose up -d --build
```

Postgres будет на `localhost:5442`, пользователь/пароль `postgres/postgres`, БД `auditdb`.

2) Запуск демо (.NET + Dapper)

```
cd src/DemoApp
export AUDIT_DEMO_CONN="Host=localhost;Port=5442;Username=postgres;Password=postgres;Database=auditdb"
dotnet run
```

Сценарий вставляет запись, делает три обновления, печатает диффы аудита и показывает снимки.

3) Запуск тестов

```
cd tests/IntegrationTests
export AUDIT_DEMO_CONN="Host=localhost;Port=5442;Username=postgres;Password=postgres;Database=auditdb"
dotnet test
```

4) Остановить стенд

```
docker compose down -v
```

## Схема и формат diff

- `audit_log` хранит:
  - `table_name`: имя аудируемой таблицы (например, `public.items`)
  - `pk`: JSON‑объект с первичным ключом(ами), например `{ "id": 1 }`
  - `ts`: метка времени изменения
  - `op`: `INSERT` | `UPDATE` | `DELETE`
  - `before` / `after`: полноценные JSONB‑снапшоты строки
  - `diff`: компактный JSON‑diff, посчитанный C‑функцией

- Формат diff (поверхностный, на уровне верхних ключей объекта):
  - `{ "set": { ... новые/изменённые ключи ... }, "unset": [ ... удалённые ключи ... ] }`
  - Для вложенных объектов сейчас применяется замена всего значения (просто и надёжно). Это упрощает применение diff при сборке снимков. Позже можно расширить на глубокие и массивные diffs.

## Стратегии сборки снимков

- Быстрый путь: `snapshot_after_at` берёт `after` из последнего изменения не позже `ts`.
- По диффам: `snapshot_from_diffs_at` стартует с самого раннего `before` и применяет диффы по порядку до `ts` (игнорируя колонку `after`). Это демонстрирует сборку из компактной истории.

## Структура проекта

```
DBMS_Course_Project/JsonDiffAuditLog/
  docker-compose.yml
  Dockerfile.postgres-pldotnet
  ext/jsondiff/
    jsondiff.c
    jsondiff.control
    jsondiff--1.0.sql
    Makefile
  db/init/
    00_extensions.sql
    01_schema.sql
    02_audit.sql
    04_triggers.sql
    05_snapshot_functions.sql
  src/DemoApp/
    DemoApp.csproj
    Program.cs
  tests/IntegrationTests/
    IntegrationTests.csproj
    AuditLogTests.cs
```

## Как это работает

- AFTER‑триггеры читают JSON из столбца `data` (`OLD.data`/`NEW.data`), вызывают C‑функцию `compute_json_diff` и записывают строку в `audit_log`.
- Diff хранит только изменения (`set`/`unset`); при сборке снимка `set` сливается через JSON‑объединение, а ключи из `unset` удаляются.

## Альтернативы

- `pgaudit`: логирует операторы, но без построчных структурированных diff.
- Парсинг WAL: мощно, но двоично и не человекочитаемо.
- Аудит на уровне приложения: гибко, но размазано; здесь логика централизована в БД.

## Сценарий демо

Демоприложение делает:
- INSERT записи с JSON‑полем
- Три UPDATE (изменение/добавление/удаление ключей)
- Читает `audit_log` и печатает диффы
- Собирает снимок на выбранный момент (двумя способами)

## Примечания и ограничения

- Diff поверхностный (по верхним ключам объектов). Глубокий diff можно добавить позже.
- `snapshot_from_diffs_at` рассчитана на объектные JSON; массивы трактуются как замена значения целиком.
- C‑расширение собирается внутри Docker через PGXS (`postgresql-server-dev-16`).

## Базируется на «Пояснительной записке»

README адаптирует идею, структуру и аргументацию из документа в репозитории, акцентируя: схему аудита, триггеры, JSON‑diff, сборку снимков, альтернативы и шаги демонстрации.
