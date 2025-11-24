# ЛР4 — Семантический анализатор (ML.NET + CNN/ONNX)

Лабораторная работа продолжает ЛР3: на тех же размеченных твитах обучаются три анализатора тональности — два классических на ML.NET и один сверточный (CNN), обученный в Python и экспортируемый в ONNX для инференса в C#.

## Сборка и развертывание
### Вариант 1 — GitHub Codespaces (рекомендуется)
- Откройте репозиторий на GitHub и нажмите `Code → Codespaces → Create codespace on develop`.
- ![Скрин: запуск Codespaces](../../docs/images/codespaces-create.png)
- После подготовки окружения уже установлены: .NET SDK, JupyterLab/nbconvert, .NET Interactive, Python, ONNX Runtime. Датасеты подтянутся автоматически.
- Важно: после старта Codespace подождите 1–2 минуты — выполняются скрипты postCreate/postStart; первые попытки запуска могут ещё не сработать.
- В терминале перейдите в папку работы: `cd MOEL/Lab4`.
- Выполните тетрадку через CLI:
  - `jupyter nbconvert --to notebook --execute src/lab4.ipynb --output lab4.exec.ipynb --output-dir src`
  - Результирующий файл: `MOEL/Lab4/src/lab4.exec.ipynb`.
- Либо откройте `MOEL/Lab4/src/lab4.ipynb` и нажмите Run All (kernel “.NET (C#)” / “polyglot-notebook” уже настроен).

**Важно — выключайте Codespace после работы**
- У бесплатных и PRO (Education) тарифов есть лимиты. Чтобы не тратить минуты, отключайте рабочее пространство: `Code → Codespaces → … → Stop codespace`.
- ![Скрин: как выключить Codespace](../../docs/images/codespaces-stop.png)

Примечание: состав контейнера и автокоманды см. в `MOEL/README.md`.

### Вариант 2 — локально в Dev Container (VS Code)
1. Подготовьте окружение: Docker Desktop, VS Code, расширение `Dev Containers`.
2. Откройте проект и выберите `Dev Containers: Reopen in Container`.
3. После сборки контейнера (≈1–2 минуты) в терминале выполните `cd MOEL/Lab4`.
4. Все зависимости ставятся автоматически в контейнере. При необходимости обновите оболочку `dotnet tool restore`.

## Использование
Перед первым запуском `src/lab4.ipynb` рекомендуется один раз обучить CNN через `src/train_cnn.ipynb`, чтобы в ноутбуке была доступна модель C (иначе выполнятся только A и B).
### Автоматический прогон
Запустить ноутбук целиком из CLI:
```bash
cd MOEL/Lab4
jupyter nbconvert --to notebook --execute src/lab4.ipynb \
  --output lab4.exec.ipynb --output-dir src
```
Собранный отчёт появится в `src/lab4.exec.ipynb`. Если `models/cnn_text.onnx` отсутствует, ноутбук выполнит только ML.NET‑анализаторы (A и B) и выведет предупреждение о пропуске CNN‑части.

### Интерактивный режим
1. Откройте `src/lab4.ipynb` в Jupyter или VS Code.
2. Выберите ядро “.NET (C#)” / “polyglot-notebook”.
3. Выполните `Run All`. Все графики, метрики и примеры предсказаний сохраняются в ноутбуке; дополнительные артефакты могут появиться в `results/`.

### Пользовательские тексты
В конце ноутбука есть блок с массивом `samples`. Туда можно добавить свои строки (отзывы, твиты) и выполнить ячейку — модели A/B (и CNN, если экспортирован ONNX) выдадут для каждого текста предсказанный класс (`negative/neutral/positive`).  
Тот же блок отрабатывает и при headless‑прогоне через `nbconvert`: примеры попадут в вывод `lab4.exec.ipynb`.

## Структура
- `src/lab4.ipynb` — основная тетрадь на C# (.NET Interactive): ML.NET‑анализаторы A/B и инференс CNN‑ONNX.
- `src/train_cnn.py` — обучение CNN в PyTorch и экспорт в ONNX + `vocab.json` + `config.json`.
- `src/train_cnn.ipynb` — тетрадь‑обёртка над `train_cnn.py` (установка зависимостей + `%run train_cnn.py`).
- `models/` — сохранённые артефакты CNN: `cnn_text.onnx`, `cnn_text.onnx.data`, `vocab.json`, `config.json`.
- `data/train.csv`, `data/test.csv` — корпус твитов с разметкой (как в ЛР3).
- `results/` — дополнительные результаты и выгрузки, формируются при выполнении ноутбука.

## Примеры работы анализаторов
### Качество на тестовом наборе
`src/lab4.ipynb` фиксирует метрики (пример одного из прогонов, Micro/Macro accuracy):

| Анализатор                         | MicroAcc | MacroAcc |
|------------------------------------|----------|----------|
| A: FeaturizeText + SDCA           | 0.79–0.82| 0.78–0.81|
| B: FeaturizeText + L-BFGS         | 0.80–0.83| 0.79–0.82|
| C: CNN (PyTorch → ONNX → C#)      | 0.77–0.84| 0.76–0.82|

Точные значения зависят от случайной инициализации, числа эпох и параметров обучения CNN. В типичных сценариях CNN даёт качество на уровне или немного выше базовых ML.NET‑моделей, особенно на более «шумных» примерах.

### Фрагменты из тестов

Примеры твитов из тестовой выборки (истинная разметка берётся из `test.csv`):

- Твит про ожидание доставки, где автор пишет, что заказ пришёл вовремя и без проблем (true label: `positive`):
  - A → `positive`
  - B → `positive`
  - C → `positive`

- Твит с жалобой на отменённый рейс и грубый сервис (true label: `negative`):
  - A → `negative`
  - B → `negative`
  - C → `negative`

На подобных «чистых» примерах все три анализатора сходятся по ответам; различия проявляются сильнее на нейтральных и смешанных по тональности сообщениях.

### Пользовательские тексты

Ниже примеры работы на трёх вручную заданных фразах (A/B — ML.NET, C — CNN‑ONNX; значения вероятностей ориентировочные):

| Текст пользователя | A: FeaturizeText + SDCA | B: FeaturizeText + L-BFGS | C: CNN (ONNX) |
|--------------------|-------------------------|---------------------------|---------------|
| "Absolutely love the latest update, everything works flawlessly and makes me so happy!" | positive (p≈0.97) | positive (p≈0.90) | positive (p≈0.85) |
| "Customer support was terrible: half of my order was missing and nobody apologized." | negative (p≈0.95) | negative (p≈0.88) | negative (p≈0.82) |
| "It's fine I guess overall, not amazing but acceptable for everyday use." | positive / neutral, в зависимости от модели | neutral (ближе к порогу) | neutral (умеренная уверенность) |

Базовые ML.NET‑модели уверенно распознают явно позитивный/негативный отзыв; CNN даёт сопоставимые ответы и чуть осторожнее относится к пограничным случаям.

## Сравнение подходов
- **A (FeaturizeText + SDCA)** — быстрый baseline на ML.NET: минимальная настройка, хорошее качество, простое развёртывание в .NET‑сервисах.
- **B (FeaturizeText + L-BFGS)** — тот же набор признаков, но другой оптимизатор; иногда даёт небольшое улучшение Micro/Macro accuracy ценой более долгого обучения.
- **C (CNN‑ONNX)** — нейросетевая модель, обученная в PyTorch и перенесённая в C# через ONNX; лучше захватывает локальные шаблоны в тексте (n‑граммы) и может быть устойчивее к «шумным» формулировкам.

ML.NET‑подходы проще для эксплуатации и интеграции, CNN требует отдельного этапа обучения, но даёт более гибкий контроль над архитектурой и потенциальный выигрыш по качеству.

## Что делает ноутбук
- Нормализует текст твитов: lowercase, удаление ссылок, очистка пунктуации, приведение пробелов.
- Формирует обучающую и тестовую выборки (`SentimentInput`) для ML.NET.
- Обучает две модели:
  - A: `FeaturizeText` → SDCA MaximumEntropy;
  - B: `FeaturizeText` → L-BFGS MaximumEntropy;
  и замеряет Micro/Macro accuracy на тесте.
- При наличии файлов в `models/`:
  - загружает `cnn_text.onnx`, `vocab.json` и `config.json`;
  - воспроизводит ту же токенизацию, что использовалась в Python;
  - считает accuracy CNN по тестовой выборке.
- Выводит предсказания всех моделей для нескольких пользовательских примеров (`samples`) прямо в конце ноутбука.

После прогонов метрики и примеры доступны в `src/lab4.exec.ipynb`, а дополнительные артефакты (если они формируются) — в `results/`.
