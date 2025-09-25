**ЛР3 — Векторные представления (LSA, Word2Vec, Doc2Vec)**

**Назначение**
- Единая предобработка: нормализация текста, токенизация, стоп-слова,
- LSA: TF-IDF по train+test → PCA → L2-нормализация → логистическая регрессия,
- Обучение собственных Word2Vec (skip-gram + negative sampling) и Doc2Vec (DBOW) представлений,
- Сравнение метрик, поиск похожих документов, проверки в 2D-проекциях.

**Структура**
- `src/lab3.ipynb` — основная .NET (C#) тетрадка.
- `data/train.csv`, `data/test.csv` — данные (Git LFS).
- `results/` — вспомогательные артефакты (графики/описания создаются при выполнении).

**Запуск (рекомендуется) — GitHub Codespaces**
- Откройте репозиторий на GitHub и нажмите `Code → Codespaces → Create codespace on develop`.
- ![Скрин: запуск Codespaces](../../docs/images/codespaces-create.png)
- В контейнере уже готовы .NET SDK, Jupyter, .NET Interactive, Git LFS.
- После старта дождитесь окончания postCreate/postStart (1–2 минуты).
- Терминал: `cd MOEL/Lab3`.
- Выполните тетрадку через CLI:
  - `jupyter nbconvert --to notebook --execute src/lab3.ipynb --output out_exec_lab3.ipynb --output-dir src`
  - Результирующий файл: `MOEL/Lab3/src/out_exec_lab3.ipynb`.
- Либо откройте `MOEL/Lab3/src/lab3.ipynb` и выполните Run All (kernel “.NET (C#)”).

**Важно — выключайте Codespace после работы**
- Минуты тарифа ограничены — останавливайте Codespace: `Code → Codespaces → … → Stop codespace`.
- ![Скрин: как выключить Codespace](../../docs/images/codespaces-stop.png)

Примечание: состав devcontainer смотрите в `MOEL/README.md`.

**Альтернатива: локально в Dev Container (VS Code)**
- Требуется Docker Desktop, VS Code, расширение `Dev Containers`.
- Откройте репозиторий и выберите `Reopen in Container`.
- После инициализации контейнера (1–2 минуты) выполните в терминале:
  `cd MOEL/Lab3 && jupyter nbconvert --to notebook --execute src/lab3.ipynb --output out_exec_lab3.ipynb --output-dir src`.
- Или откройте тетрадку и нажмите Run All.
- Завершение: `Dev Containers: Close Remote`.

**Что делает тетрадка**
- Предобработка и выборка ~4k train документов (для ускорения обучения) + все test.
- LSA: TF-IDF → PCA (300 компонент) → нормализация → логистическая регрессия, метрики train/test.
- Word2Vec: собственная реализация skip-gram с негативной выборкой (2 эпохи, vocab min-count=5), усреднение слов в документе, классификация и сравнение метрик.
- Doc2Vec: DBOW-подобная модель (4 негативных примера, 3 эпохи), прямые документные вектора, классификация.
- Поиск ближайших train-документов к выбранному test-документу для каждого представления.
- Быстрая сводка о 2D-проекциях (через PCA) и статистика по компонентам.

После запуска метрики и текстовые примеры доступны прямо в выводе, а выполненная тетрадка сохраняется как `src/out_exec_lab3.ipynb`.
