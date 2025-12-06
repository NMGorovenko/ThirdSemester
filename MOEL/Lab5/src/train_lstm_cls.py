import os
import json
import re
from pathlib import Path

import torch
import torch.nn as nn
from torch.utils.data import Dataset, DataLoader

DATA_DIR = Path(__file__).resolve().parents[1] / "data"
MODEL_DIR = Path(__file__).resolve().parents[1] / "models"
MODEL_DIR.mkdir(parents=True, exist_ok=True)

TRAIN_CSV = DATA_DIR / "train.csv"
TEST_CSV = DATA_DIR / "test.csv"


def normalize(text: str) -> str:
    text = text.lower()
    text = re.sub(r"https?://\S+", " ", text)
    text = re.sub(r'["\'`]+', "", text)
    text = re.sub(r"[^0-9a-z#@ ]+", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text


def read_csv(path: Path, with_label: bool = True):
    import csv

    rows = []
    with path.open("r", encoding="utf-8", errors="ignore") as f:
        reader = csv.DictReader(f)
        for row in reader:
            text = row.get("text") or ""
            if not text:
                continue
            text_norm = normalize(text)
            if with_label:
                label = (row.get("sentiment") or "").strip()
                if not label:
                    continue
                rows.append((text_norm, label))
            else:
                rows.append(text_norm)
    return rows


print(f"Reading data from {TRAIN_CSV} / {TEST_CSV}")
train_rows = read_csv(TRAIN_CSV, with_label=True)
test_rows = read_csv(TEST_CSV, with_label=True)
print(f"Train={len(train_rows)}  Test={len(test_rows)}")

label_to_id = {"negative": 0, "neutral": 1, "positive": 2}
id_to_label = {v: k for k, v in label_to_id.items()}


def build_vocab(texts, max_vocab: int = 20000, min_freq: int = 2):
    from collections import Counter

    counter = Counter()
    for s in texts:
        counter.update(s.split())

    items = [w for w, f in counter.items() if f >= min_freq]
    items.sort(key=lambda w: counter[w], reverse=True)
    items = items[: max_vocab - 2]

    vocab = {"<pad>": 0, "<unk>": 1}
    for i, w in enumerate(items, start=2):
        vocab[w] = i
    return vocab


vocab = build_vocab([t for t, _ in train_rows])
vocab_size = len(vocab)
max_len = 40


def to_ids(text: str):
    tokens = text.split()
    ids = [vocab.get(t, 1) for t in tokens][:max_len]
    if len(ids) < max_len:
        ids += [0] * (max_len - len(ids))
    return ids


class SentimentDataset(Dataset):
    def __init__(self, rows):
        self.inputs = [to_ids(t) for t, _ in rows]
        self.labels = [label_to_id[y] for _, y in rows]

    def __len__(self):
        return len(self.inputs)

    def __getitem__(self, idx):
        x = torch.tensor(self.inputs[idx], dtype=torch.long)
        y = torch.tensor(self.labels[idx], dtype=torch.long)
        return x, y


train_ds = SentimentDataset(train_rows)
test_ds = SentimentDataset(test_rows)
train_dl = DataLoader(train_ds, batch_size=128, shuffle=True)
test_dl = DataLoader(test_ds, batch_size=256)


class LSTMClassifier(nn.Module):
    def __init__(self, vocab_size: int, emb_dim: int = 100, hidden_dim: int = 128, num_layers: int = 1, num_classes: int = 3):
        super().__init__()
        self.emb = nn.Embedding(vocab_size, emb_dim, padding_idx=0)
        self.lstm = nn.LSTM(emb_dim, hidden_dim, num_layers=num_layers, batch_first=True)
        self.fc = nn.Linear(hidden_dim, num_classes)

    def forward(self, x):
        emb = self.emb(x)  # [B, L, D]
        # Use the last time-step from the sequence instead of h_n[-1]
        # This avoids aten.unbind in the graph and exports to ONNX more reliably.
        out, _ = self.lstm(emb)  # out: [B, L, H]
        last = out[:, -1, :]  # [B, H]
        logits = self.fc(last)
        return logits


device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device}")

model = LSTMClassifier(vocab_size=vocab_size).to(device)
optimizer = torch.optim.Adam(model.parameters(), lr=2e-3)
criterion = nn.CrossEntropyLoss()


def run_epoch(dataloader, train: bool = True):
    if train:
        model.train()
    else:
        model.eval()

    total = 0
    correct = 0
    loss_sum = 0.0

    for x, y in dataloader:
        x = x.to(device)
        y = y.to(device)

        if train:
            optimizer.zero_grad()

        logits = model(x)
        loss = criterion(logits, y)

        if train:
            loss.backward()
            optimizer.step()

        loss_sum += loss.item() * y.size(0)
        pred = logits.argmax(dim=1)
        correct += (pred == y).sum().item()
        total += y.size(0)

    avg_loss = loss_sum / max(1, total)
    acc = correct / max(1, total)
    return avg_loss, acc


for epoch in range(1, 9):
    train_loss, train_acc = run_epoch(train_dl, train=True)
    test_loss, test_acc = run_epoch(test_dl, train=False)
    print(
        f"[LSTM-CLS] epoch {epoch}: "
        f"train loss={train_loss:.4f}, acc={train_acc:.3f} | "
        f"test loss={test_loss:.4f}, acc={test_acc:.3f}"
    )


# Export ONNX
onnx_path = MODEL_DIR / "lstm_cls.onnx"
config = {"max_len": max_len}

with (MODEL_DIR / "lstm_cls_config.json").open("w", encoding="utf-8") as f:
    json.dump(config, f)

with (MODEL_DIR / "lstm_cls_vocab.json").open("w", encoding="utf-8") as f:
    json.dump(vocab, f, ensure_ascii=False)

dummy = torch.randint(0, vocab_size, (1, max_len), dtype=torch.long, device=device)

try:
    torch.onnx.export(
        model,
        dummy,
        onnx_path.as_posix(),
        input_names=["input_ids"],
        output_names=["logits"],
        dynamic_axes={"input_ids": {0: "batch"}, "logits": {0: "batch"}},
        opset_version=13,
        dynamo=False,
    )
    print(f"Exported LSTM classifier ONNX to {onnx_path}")
except Exception as e:
    print(f"WARNING: failed to export LSTM classifier to ONNX ({e}). The .pt model is still trained; try an older torch/onnx version if ONNX is required.")
