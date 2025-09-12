**ЛР2 — LSA (TF‑IDF → PCA → нормализация → логистическая регрессия)**

**Назначение**
- Построение признаков TF‑IDF на общем корпусе (train+test),
- Снижение размерности PCA (аналог LSA),
- L2‑нормализация признаков,
- Обучение логистической регрессии и визуализации.

**Структура**
- `src/lab2.ipynb` — основная .NET (C#) тетрадка.
- `data/train.csv`, `data/test.csv` — данные (Git LFS).

**Запуск (рекомендуется) — GitHub Codespaces**
- Откройте репозиторий на GitHub и нажмите `Code → Codespaces → Create codespace on develop`.
- ![Скрин: запуск Codespaces](../../docs/images/codespaces-create.png)
- После подготовки окружения всё уже установлено: .NET SDK, JupyterLab/nbconvert, .NET Interactive, Git LFS. Датасеты подтянутся автоматически (в контейнере выполняется `git lfs pull`).
- В терминале перейдите в папку работы: `cd MOEL/Lab2`.
- Выполните тетрадку через CLI:
  - `jupyter nbconvert --to notebook --execute src/lab2.ipynb --output out_exec_lab2.ipynb --output-dir src`
  - Результирующий файл: `MOEL/Lab2/src/out_exec_lab2.ipynb`.
- Либо откройте `MOEL/Lab2/src/lab2.ipynb` и нажмите Run All (kernel “.NET (C#)” уже настроен).

**Важно — выключайте Codespace после работы**
- У бесплатных и PRO (Education) тарифов есть лимиты. Чтобы не тратить минуты, отключайте рабочее пространство: `Code → Codespaces → … → Stop codespace`.
- ![Скрин: как выключить Codespace](../../docs/images/codespaces-stop.png)


Примечание: состав контейнера и автокоманды см. в `MOEL/README.md`.

**Использование и примеры результатов**
- Тетрадка печатает:
  - Размерности TF‑IDF и разреженность,
  - Размерность LSA‑признаков,
  - Метрики на train и test: Accuracy, F1, AUC, Precision/Recall по классам,
  - Графики: накопленный % информации (аналог explained_variance_ratio_), Scatter 1 vs 5 и 2 vs 4 компонент.
- В конце печатаются 7 примеров:
  - `Original sentence`, `Transformed sentence`, `Actual label`, `Predicted label`.
