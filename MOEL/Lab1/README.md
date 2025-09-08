**Лабораторная работа №1 — Токенизация и анализ тональности**

**Задача**
- Реализовать токенизацию и нормализацию (стоп‑слова, простая лемматизация),
- Построить анализатор тональности (наивный Байес),
- Визуализировать частотности.

**Структура**
- `src/lab1.ipynb` — основная .NET (C#) тетрадка.
- `data/` — CSV (Git LFS).

**Требования**
- .NET SDK 8.0+
- JupyterLab и .NET Interactive kernel (или VS Code + Polyglot Notebook)
- Git LFS

**Установка (macOS)**
- `brew install git git-lfs dotnet jupyterlab`
- `git lfs install`
- `git lfs pull`
- Kernel: `dotnet tool update -g Microsoft.dotnet-interactive && ~/.dotnet/tools/dotnet-interactive jupyter install`

**Установка (Ubuntu 22.04)**
- .NET 9 SDK: - можно скачать с сайта sdk https://dotnet.microsoft.com/en-us/download/dotnet/9.0
  - `sudo apt-get update && sudo apt-get install -y wget apt-transport-https`
  - `wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb`
  - `sudo apt-get update && sudo apt-get install -y dotnet-sdk-0.0`
- Jupyter: `sudo apt-get install -y python3-pip && pip3 install --user jupyterlab`
- Git LFS: `sudo apt-get install -y git-lfs && git lfs install`
- Клонирование и данные: `git lfs pull`
- Kernel (https://github.com/dotnet/interactive/blob/main/docs/NotebookswithJupyter.md): `dotnet tool update -g Microsoft.dotnet-interactive && dotnet interactive jupyter install`

**Запуск**
- CLI (без IDE):
  - `jupyter nbconvert --to notebook --execute Lab1/src/lab1.ipynb --output out_exec_lab1.ipynb --output-dir Lab1/src`
  - Откройте `Lab1/src/out_exec_lab1.ipynb` для графиков и результатов.
- VS Code + Polyglot:
  - Откройте `Lab1/src/lab1.ipynb`, выберите kernel “.NET (C#)”, Run All.

**Использование и примеры**
- В тетрадке:
  - Чтение `data/train.csv`/`data/test.csv`, фильтрация,
  - Токенизация, удаление стоп‑слов, простая нормализация,
  - Обучение наивного Байеса и печать метрик (Accuracy/F1 и др.),
  - Визуализации распределений.

**Пример работы**
```
original sentence: Shanghai is also really exciting (precisely -- skyscrapers galore). Good tweeps in China:  (SH)  (BJ).
transformed sentence: shanghai also real excit precise skyscraper galore good tweep china sh bj
label: 1
model prediction: positive

original sentence: Recession hit Veronique Branquinho, she has to quit her company, such a shame!
transformed sentence: recession hit veronique branquinho she quit company such shame
label: -1
model prediction: negative

original sentence: happy bday!
transformed sentence: happy bday
label: 1
model prediction: positive

original sentence: http://twitpic.com/4w75p - I like it!!
transformed sentence: http twitpic com like
label: 1
model prediction: positive

original sentence: that`s great!! weee!! visitors!
transformed sentence: great weee visitor
label: 1
model prediction: positive

original sentence: I THINK EVERYONE HATES ME ON HERE   lol
transformed sentence: think everyone hate here lol
label: -1
model prediction: negative

original sentence: soooooo wish i could, but im in school and myspace is completely blocked
transformed sentence: soooooo wish but im school myspace complete block
label: -1
model prediction: negative
```

