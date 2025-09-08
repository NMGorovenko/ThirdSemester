**ЛР2 — LSA (TF‑IDF → PCA → нормализация → логистическая регрессия)**

**Назначение**
- Построение признаков TF‑IDF на общем корпусе (train+test),
- Снижение размерности PCA (аналог LSA),
- L2‑нормализация признаков,
- Обучение логистической регрессии и визуализации.

**Структура**
- `src/lab2.ipynb` — основная .NET (C#) тетрадка.
- `data/train.csv`, `data/test.csv` — данные (Git LFS).

**Требования**
- .NET SDK 8.0+ и JupyterLab
- .NET Interactive kernel для Jupyter (`Microsoft.dotnet-interactive`)
- Git LFS для загрузки CSV

**Установка (macOS)**
- `brew install git git-lfs dotnet jupyterlab`
- `git lfs install`
- Клон: `git clone <repo-url> && cd <repo-root> && git lfs pull`
- Kernel: `dotnet tool update -g Microsoft.dotnet-interactive && dotnet interactive jupyter install`

**Установка (Ubuntu 22.04)**
- .NET 8 SDK:
  - `sudo apt-get update && sudo apt-get install -y wget apt-transport-https`
  - `wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb`
  - `sudo apt-get update && sudo apt-get install -y dotnet-sdk-9.0`
- Jupyter: `sudo apt-get install -y python3-pip && pip3 install --user jupyterlab`
- Git LFS: `sudo apt-get install -y git-lfs && git lfs install`
- Клонировать: `git lfs pull`
- Kernel: `dotnet tool update -g Microsoft.dotnet-interactive && dotnet interactive jupyter install`

**Запуск (CLI, без IDE)**
- `jupyter nbconvert --to notebook --execute Lab2/src/lab2.ipynb --output out_exec_lab2.ipynb --output-dir Lab2/src`
- Результат и графики будут в `Lab2/src/out_exec_lab2.ipynb`.

**Запуск (VS Code + Polyglot Notebook)**
- Установить VS Code и расширение “Polyglot Notebook”.
- Открыть `Lab2/src/lab2.ipynb`, выбрать kernel “.NET (C#)”.
- Run All.

**Использование и примеры результатов**
- Тетрадка печатает:
  - Размерности TF‑IDF и разреженность,
  - Размерность LSA‑признаков,
  - Метрики на train и test: Accuracy, F1, AUC, Precision/Recall по классам,
  - Графики: накопленный % информации (аналог explained_variance_ratio_), Scatter 1 vs 5 и 2 vs 4 компонент.
- В конце печатаются 7 примеров:
  - `Original sentence`, `Transformed sentence`, `Actual label`, `Predicted label`.

