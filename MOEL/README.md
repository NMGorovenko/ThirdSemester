**Проект ЛР по МОЭЛ (C# + .NET Interactive)**

- **Структура:**
  - `Lab*/data/` — датасеты (CSV), подключены через Git LFS
  - `Lab*/src/lab*.ipynb` — Jupyter‑тетрадки с выполненными работами

**Рекомендуемый способ запуска — GitHub Codespaces (devcontainer)**
- Откройте репозиторий на GitHub и нажмите `Code → Codespaces → Create codespace on develop`.
- ![Скрин: запуск Codespaces](../docs/images/codespaces-create.png)
- После подготовки окружения всё уже установлено: .NET SDK, JupyterLab/nbconvert, .NET Interactive, Git LFS. Датасеты подтянутся автоматически.
- Важно: после старта Codespace подождите 1–2 минуты — устанавливаются расширения и выполняются скрипты postCreate/postStart; сразу после запуска некоторые команды/ядеро могут ещё не работать.

**Состав devcontainer (коротко)**
- База: `mcr.microsoft.com/devcontainers/dotnet:9.0` (SDK 9).
- Features: `common-utils`, `python 3.11`, `git-lfs`.
- Инструменты: JupyterLab + nbconvert (pip, user), .NET Interactive (global tool) с установленным Jupyter‑kernel, Git LFS.
- VS Code extensions: dotnet-interactive, Jupyter (+keymap, renderers), Python.
- Пользователь: `vscode`; PATH дополнен `~/.local/bin` и `~/.dotnet/tools`.

**Что автоматически выполняется**
- PostCreate: обновление pip, установка `jupyterlab nbconvert`, установка/обновление `Microsoft.dotnet-interactive`, `dotnet interactive jupyter install`.
- PostStart: `git pull` (синхронизация репозитория) и `git lfs pull` (загрузка датасетов).

Точные команды (см. `.devcontainer/devcontainer.json`):
```
export PATH="$HOME/.local/bin:$PATH"
python -m pip install --upgrade pip --no-input
python -m pip install --no-input --user jupyterlab nbconvert
dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive
dotnet interactive jupyter install
```
и при старте контейнера:
```
git lfs pull
```

**Почему devcontainers / Codespaces**
- Повторяемость: один и тот же образ и инструменты для всех.
- Нулевая установка локально: всё готово в облаке.
- Быстрый старт: kernel и LFS преднастроены, данные подтягиваются сами.
- Изоляция и чистота окружения; легко остановить и не тратить ресурсы.

**Как выполнить тетрадки в Codespaces (через терминал)**
- Откройте терминал в Codespaces.
- Для каждой работы перейдите в её папку:
  - ЛР1: `cd MOEL/Lab1 && jupyter nbconvert --to notebook --execute src/lab1.ipynb --output out_exec_lab1.ipynb --output-dir src`
  - ЛР2: `cd MOEL/Lab2 && jupyter nbconvert --to notebook --execute src/lab2.ipynb --output out_exec_lab2.ipynb --output-dir src`
- Результаты: `MOEL/Lab1/src/out_exec_lab1.ipynb`, `MOEL/Lab2/src/out_exec_lab2.ipynb`.

**Как выполнить тетрадки в UI (в Codespaces)**
- Откройте нужный `.ipynb` (например, `MOEL/Lab1/src/lab1.ipynb`).
- Вверху нажмите Run All — kernel “.NET (C#)” уже преднастроен.
- Если сразу не работает, подождите 1–2 минуты после старта Codespace и повторите.

**Важно — выключайте Codespace после работы**
- У бесплатных и PRO (Education) тарифов есть лимиты. Чтобы не тратить минуты, отключайте рабочее пространство: `Code → Codespaces → … → Stop codespace`.
- ![Скрин: как выключить Codespace](../docs/images/codespaces-stop.png)

**Примечания по данным (Git LFS)**
- Если вдруг видите “указатели” вместо CSV, выполните вручную: `git lfs pull`.

**Локально в Dev Container (VS Code)**
- Зачем: воспроизводимое окружение без локальных установок SDK и Python; всё ставится в контейнере.
- Требуется: Docker Desktop (или Docker + WSL2 на Windows), VS Code, расширение `Dev Containers`.
- Клонируйте репозиторий и откройте его в VS Code.
- В появившемся баннере нажмите `Reopen in Container` (или через палитру: `Dev Containers: Reopen in Container`).
- Подождите 1–2 минуты: сборка контейнера, установка расширений, запуск postCreate/postStart.
- Дальшее:
  - Терминал: `cd MOEL/Lab*` и выполните команды nbconvert из раздела выше.
  - **Или** откройте нужный `.ipynb` и `Run All`.
- Завершение: `Dev Containers: Close Remote` (VS Code закроет контейнер и высвободит ресурсы).
