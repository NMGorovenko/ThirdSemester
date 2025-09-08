**Проект ЛР по МОЭЛ (C# + .NET Interactive)**

- **Структура:**
  - `Lab*/data/` — датасеты (CSV), подключены через Git LFS
  - `Lab*/src/lab*.ipynb` — Jupiter notebook с выполненной работой

**Быстрый старт (macOS, с нуля)**
- Установки: `brew install git git-lfs dotnet jupyterlab`
- Инициализировать LFS: `git lfs install`
- Скачать данные LFS: `git lfs pull`
- Поставить .NET Interactive kernel: `dotnet tool update -g Microsoft.dotnet-interactive && dotnet interactive jupyter install`

**Быстрый старт (Ubuntu 22.04, с нуля)**
- .NET 9 SDK:
  - `sudo apt-get update && sudo apt-get install -y wget apt-transport-https`
  - `wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb`
  - `sudo apt-get update && sudo apt-get install -y dotnet-sdk-9.0`
- Jupyter: `sudo apt-get install -y python3-pip && pip3 install --user jupyterlab`
- Git LFS: `sudo apt-get install -y git-lfs && git lfs install`
- Подтянуть датасеты: `git lfs pull`
- .NET Interactive kernel: `dotnet tool update -g Microsoft.dotnet-interactive && dotnet interactive jupyter install`
- Запустить на примере первой работы через CLI: `jupyter nbconvert --to notebook --execute Lab1/src/lab1.ipynb --output out_exec_lab1.ipynb --output-dir Lab1/src`

**Альтернатива: VS Code + Polyglot Notebook**
- Поставить VS Code и расширение “Polyglot Notebook”.
- Открыть `.ipynb` (например, `Lab1/src/lab1.ipynb`).
- Выбрать kernel “.NET (C#)”.
- Запустить все ячейки (Run All).

**Примечания по данным (Git LFS)**
- Датасеты хранятся в LFS. После клонирования обязательно выполните `git lfs pull`.
- Если LFS не установлен, CSV‑файлы будут маленькими “указателями” и код не даст корректных результатов.

**Где смотреть результаты**
- После успешного прогона через nbconvert в `Lab1/src/out_exec_lab1.ipynb` и аналогично для ЛР1.
- Внутри тетрадок есть печать метрик и визуализации.
