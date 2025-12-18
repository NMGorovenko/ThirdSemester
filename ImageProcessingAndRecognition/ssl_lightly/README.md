# LightlySSL (SimCLR) — самодостаточный архив для проверки

Содержимое:
- `Lightly_SimCLR_Pretrain_and_Retrieval.ipynb` — self-supervised предобучение SimCLR на изображениях без разметки + downstream задача CV: поиск похожих изображений (retrieval).
- `requirements.txt` — зависимости для авто-установки из ноутбука.
- `dataset/images/train`, `dataset/images/val` — локальная копия датасета (чтобы отправить одним архивом).
- `REPORT.md` — подробный отчет.

Как проверить:
1) Распаковать архив.
2) Открыть `Lightly_SimCLR_Pretrain_and_Retrieval.ipynb` в Jupyter / JupyterLab.
3) Нажать **Run All**.

Результат:
- В конце появится `output/retrieval_grid.png` (query + топ похожих изображений).

