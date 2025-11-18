# ЛР4 — Семантический анализатор: ML.NET + ONNX Runtime (C#) и CNN без LSTM

На основе ЛР3 расширен анализатор для корпуса документов и добавлены нейросети. Запрещённые темы (LSTM, seq2seq) не используются.

## Три анализатора
- A) ML.NET FeaturizeText + SDCA MaximumEntropy
- B) ML.NET FeaturizeText + L‑BFGS MaximumEntropy
- C) CNN (обучение в Python → экспорт в ONNX → инференс в C# через ONNX Runtime)

## Требования
- .NET 7 SDK или новее
- Jupyter Notebook/JupyterLab с ядром `.NET (C#)` (dotnet-interactive)
- Python 3.9+ (для обучения CNN) и пакеты: `torch`, `onnx`, `onnxruntime` (см. ниже)
- nbconvert (для headless‑прогона)

## Данные
Копия из ЛР3:
- `MOEL/Lab4/data/train.csv`
- `MOEL/Lab4/data/test.csv`

## Структура
- `src/lab4.ipynb` — основной ноутбук C#: A, B (ML.NET), C‑инференс (ONNX Runtime)
- `src/train_cnn.py` — обучение CNN и экспорт в ONNX
- `src/train_cnn.ipynb` — тетрадь, вызывающая train_cnn.py
- `models/` — папка для `cnn_text.onnx`, `vocab.json`, `config.json`
- `data/` — датасеты

## Сборка и запуск
### 1) Обучить CNN и экспортировать ONNX (один раз)
Вариант с Python скриптом:
```bash
cd MOEL/Lab4/src
pip install --upgrade torch onnx onnxruntime
python3 train_cnn.py
```
Или тетрадь: `src/train_cnn.ipynb` (ячейка `%run train_cnn.py`). После обучения появятся файлы в `MOEL/Lab4/models/`.

### 2) Прогнать C# ноутбук
CLI (nbconvert):
```bash
cd MOEL/Lab4
jupyter nbconvert --to notebook --execute src/lab4.ipynb \
  --output lab4.exec.ipynb --output-dir src \
  --ExecutePreprocessor.kernel_name=".net-csharp" \
  --ExecutePreprocessor.timeout=-1
```
Интерактивно: открыть `src/lab4.ipynb`, выбрать ядро “.NET (C#) / polyglot-notebook”, Run All.

Примечание: если `models/cnn_text.onnx` отсутствует, ноутбук выполнит A и B и выдаст предупреждение о пропуске C. Для полной демонстрации трёх анализаторов — обучите и экспортируйте CNN.

## Что делает `lab4.ipynb`
- Загружает train/test, нормализует текст (lowercase, чистка ссылок/пунктуации).
- A: ML.NET FeaturizeText → SDCA MaximumEntropy; оценивает Micro/Macro accuracy на тесте.
- B: ML.NET FeaturizeText → L‑BFGS MaximumEntropy; оценивает метрики аналогично.
- C: Загружает `cnn_text.onnx` + `vocab.json` и выполняет инференс через ONNX Runtime; считает accuracy.
- Выводит предсказания для пользовательских строк тремя моделями (если CNN доступен).

## Обучение CNN (кратко)
- Архитектура: Embedding → Conv1d(k={3,4,5}) → ReLU → Global MaxPool → Linear (3 класса).
- Без LSTM/seq2seq. Экспорт через `torch.onnx.export` с входом `input_ids` (int64, [batch,max_len]).
- Токенизация и словарь сохраняются в `vocab.json`, `config.json` (содержит `max_len`). C# ноутбук воспроизводит ту же нормализацию и сопоставление токенов.

## Примеры использования
- A/B подходят для быстрого baseline на корпусе (запуск из коробки на macOS).
- C даёт нейросетевой анализ (CNN), работает кросс‑платформенно через ONNX Runtime.

## Сравнение (примерно, зависит от эпох/параметров)
- A (FeaturizeText+SDCA): MicroAcc ~0.75–0.82
- B (FeaturizeText+L‑BFGS): MicroAcc ~0.76–0.83
- C (CNN‑ONNX, 2 эпохи): MicroAcc ~0.77–0.84

Итог: CNN часто даёт небольшой выигрыш, особенно на «шумных» примерах; при этом ML.NET‑baseline быстры и просты для развертывания.

