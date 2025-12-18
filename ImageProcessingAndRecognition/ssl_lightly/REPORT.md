# Дополнительное задание: Self-Supervised предобучение (LightlySSL)

Источник: https://docs.lightly.ai/self-supervised-learning/index.html

## 1. Цель работы

Сделать self-supervised предобучение (без разметки) для компьютерного зрения с помощью библиотеки **LightlySSL**, а затем применить полученную модель к прикладной задаче CV.

Я выбрал связку:

1) **Self-supervised pre-training (SimCLR)** на наборе изображений без меток.  
2) **Downstream задача: поиск похожих изображений (image retrieval / similarity search)** по эмбеддингам (cosine similarity + top-k).

Почему это хороший вариант для “доп. задания”:
- downstream не требует ручной разметки (можно сделать полностью на своих данных),
- наглядный результат (для каждого изображения показываем ближайшие “похожие”),
- типичный сценарий использования SSL: сначала учим “универсальные” признаки, потом решаем задачу.

## 2. Коротко о self-supervised learning (что это и зачем)

**Self-supervised learning** — это обучение представлений на данных без разметки за счет “искусственных” задач (pretext tasks). Идея: сеть учится выделять устойчивые признаки (форму/структуру/текстуры), которые потом помогают в обычных задачах (классификация, детекция, сегментация, поиск).

Основные семейства методов:
- **Contrastive** (SimCLR, MoCo): сближаем представления двух аугментированных версий одного изображения и раздвигаем представления разных изображений.
- **Non-contrastive / distillation** (BYOL, SimSiam, DINO): учим student повторять teacher (или stop-grad), часто без явных “негативов”.
- **Masked modeling** (MAE/SimMIM): восстанавливаем скрытые части изображения.

В этой работе использован **SimCLR**, потому что он прост в реализации и есть готовые компоненты в Lightly.

## 3. Данные (что можно взять из этого репозитория)

В папке `ImageProcessingAndRecognition` уже есть изображения, которые подходят для self-supervised предобучения:

### 3.1. BSDS-подобный датасет (много изображений, удобно для SSL)

Пути:
- `ImageProcessingAndRecognition/Dataset/images/train`
- `ImageProcessingAndRecognition/Dataset/images/val`
- `ImageProcessingAndRecognition/Dataset/images/test`

Эти данные хорошо подходят для SSL, потому что **изображений достаточно много**, а разметку можно вообще не использовать.

### 3.2. Данные из лабораторной 5 (маленький датасет + маски)

Пути:
- `ImageProcessingAndRecognition/lab5/data/images`
- `ImageProcessingAndRecognition/lab5/data/masks`

Идея: на `images/` можно сделать SSL предобучение, а потом (опционально) fine-tune под сегментацию с `masks/`.

## 4. Инструменты и окружение

### 4.1. Что нужно установить

- Python 3.10–3.12 (рекомендуется для экосистемы PyTorch; Python 3.13 может не иметь готовых wheels для `torch` на вашей платформе).
- PyTorch + torchvision (CPU или CUDA).
- LightlySSL (`lightly`).
- Базовые утилиты: `numpy`, `pillow`, `tqdm`, `matplotlib`.

В репозитории добавлены скрипты и список зависимостей:
- `ImageProcessingAndRecognition/ssl_lightly/requirements.txt`
- `ImageProcessingAndRecognition/ssl_lightly/pretrain_simclr.py`
- `ImageProcessingAndRecognition/ssl_lightly/embed_and_retrieve.py`

### 4.2. Пример установки (вариант через venv)

Команды (пример, под macOS/Linux):

```bash
python3.11 -m venv .venv-ssl
source .venv-ssl/bin/activate

# 1) поставить PyTorch (выберите вариант с CPU/CUDA по вашей ОС/видеокарте)
# см. https://pytorch.org/get-started/locally/

# 2) поставить остальное
pip install -r ImageProcessingAndRecognition/ssl_lightly/requirements.txt
```

Проверка:

```bash
python -c "import torch; import lightly; print(torch.__version__)"
```

## 5. Этап 1 — Self-Supervised pre-training (SimCLR)

### 5.1. Идея SimCLR

Для каждого изображения делаем **две сильные аугментации** (две “view” одного и того же объекта). Сеть должна:
- сделать их эмбеддинги **похожими** (positive pair),
- а эмбеддинги разных изображений — **непохожими** (negative pairs в батче).

Технически:
- backbone (например, ResNet-18) → признаки,
- projection head (MLP) → проекция в пространство SSL,
- loss: `NTXentLoss` (InfoNCE-подобный).

### 5.2. Запуск предобучения на ваших данных

Вариант A (рекомендуется): больше данных, лучше для SSL:

```bash
python ImageProcessingAndRecognition/ssl_lightly/pretrain_simclr.py \
  --data-dir ImageProcessingAndRecognition/Dataset/images/train \
  --out-dir runs/simclr_bsds_train \
  --backbone resnet18 \
  --img-size 224 \
  --batch-size 128 \
  --epochs 50 \
  --lr 0.3 \
  --temperature 0.5 \
  --amp
```

Вариант B: маленькие данные (скорее демо/проверка пайплайна):

```bash
python ImageProcessingAndRecognition/ssl_lightly/pretrain_simclr.py \
  --data-dir ImageProcessingAndRecognition/lab5/data/images \
  --out-dir runs/simclr_lab5 \
  --backbone resnet18 \
  --img-size 224 \
  --batch-size 64 \
  --epochs 100 \
  --lr 0.1 \
  --temperature 0.5 \
  --amp
```

### 5.3. Что получается на выходе

В `--out-dir` сохраняется:
- `checkpoint_last.pt` — веса backbone + projection head (и optimizer), чтобы продолжать обучение
- `backbone_only.pt` — только backbone (удобно для downstream)
- `run_meta.json` — параметры запуска (для отчета/воспроизводимости)

## 6. Этап 2 — Downstream задача: поиск похожих изображений

### 6.1. Суть задачи

Мы хотим: для заданного изображения найти **k наиболее похожих** в нашем датасете.

Подход:
1) прогоняем все изображения через предобученный backbone → получаем эмбеддинги,
2) L2-нормируем эмбеддинги,
3) считаем cosine similarity (это просто скалярное произведение нормированных векторов),
4) берем `top-k` соседей.

Это типичная практическая задача CV (поиск, дедупликация, кластеризация, визуальная навигация).

### 6.2. Запуск retrieval

```bash
python ImageProcessingAndRecognition/ssl_lightly/embed_and_retrieve.py \
  --data-dir ImageProcessingAndRecognition/Dataset/images/val \
  --checkpoint runs/simclr_bsds_train/checkpoint_last.pt \
  --out-dir runs/retrieval_bsds_val \
  --img-size 224 \
  --topk 6 \
  --num-queries 5
```

Выход:
- `runs/retrieval_bsds_val/retrieval_grid.png` — картинка “query + топ похожих”
- `runs/retrieval_bsds_val/embeddings.pt` — эмбеддинги
- `runs/retrieval_bsds_val/paths.json` — пути к изображениям
- `runs/retrieval_bsds_val/retrieval.json` — индексы соседей и значения похожести

## 7. Как оформить результат в отчете (что показать преподавателю)

Минимальный набор, который обычно ожидают:

1) **Описание метода** (SimCLR: две аугментации, contrastive loss, projection head).
2) **Описание данных** (какие изображения использовали, сколько, откуда).
3) **Параметры обучения** (backbone, img_size, batch_size, epochs, lr, temperature).
4) **Что получилось**:
   - график/таблица лосса по эпохам (можно просто значения из консоли),
   - `retrieval_grid.png` с примерами похожих изображений.
5) **Вывод**: что дает SSL (модель учится общим визуальным признакам без разметки).

Шаблон для вставки фактов (заполнить после прогона):

```text
Датасет pretrain: __________________
Кол-во изображений: _________________
Backbone: ___________________________
Epochs / Batch / LR / Temp: _________
Итоговый train loss: ________________
Downstream: image retrieval (top-k) ✅
Пример результата: runs/.../retrieval_grid.png
```

## 8. Что можно улучшить (если хотите “плюс баллы”)

### 8.1. Сравнение с baseline

Сделать retrieval **без предобучения** (случайная инициализация backbone) и сравнить:
- визуально (картинки соседей),
- среднюю похожесть top-k,
- или “ручную” оценку (похожи ли сцены).

### 8.2. Другой SSL-метод из Lightly

Попробовать вместо SimCLR:
- MoCo (обычно устойчивее на меньших batch),
- DINO (часто сильный для transfer).

Идея отчета: одинаковые данные, одинаковое число эпох → сравнить retrieval визуально.

### 8.3. Downstream “с разметкой” (опционально)

Если нужно строго “под задачу” типа классификация/сегментация:
- взять `ImageProcessingAndRecognition/lab5/data/images` + `.../masks`,
- предобучить backbone без масок,
- затем fine-tune сегментационную модель (UNet/DeepLab) на малом числе разметки,
- сравнить качество с обучением “с нуля”.

## 9. Ссылки

- LightlySSL docs: https://docs.lightly.ai/self-supervised-learning/index.html
- Пример SimCLR в LightlySSL: https://docs.lightly.ai/self-supervised-learning/examples/simclr.html
- SimCLR paper: https://arxiv.org/abs/2002.05709

