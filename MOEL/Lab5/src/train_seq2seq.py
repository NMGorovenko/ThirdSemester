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


def normalize(text: str) -> str:
    text = text.lower()
    text = re.sub(r"https?://\S+", " ", text)
    text = re.sub(r'["\'`]+', "", text)
    text = re.sub(r"[^0-9a-z#@ ]+", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text


def read_rows(path: Path):
    import csv

    rows = []
    with path.open("r", encoding="utf-8", errors="ignore") as f:
        reader = csv.DictReader(f)
        for row in reader:
            text = (row.get("selected_text") or "").strip()
            sentiment = (row.get("sentiment") or "").strip()
            time = (row.get("Time of Tweet") or "").strip()
            age = (row.get("Age of User") or "").strip()
            country = (row.get("Country") or "").strip()
            if not text or not sentiment:
                continue
            text_norm = normalize(text)
            rows.append((text_norm, sentiment, time, age, country))
    return rows


train_rows = read_rows(TRAIN_CSV)
print(f"Seq2seq train rows: {len(train_rows)}")


def make_tokens(text: str, sentiment: str, time: str, age: str, country: str):
    sem_tokens = [
        "<bos>",
        f"sent_{sentiment.lower()}",
        f"time_{time.lower()}",
        f"age_{age}",
        "country_" + re.sub(r"[^a-z]+", "_", country.lower()).strip("_"),
    ]
    text_tokens = text.split()
    tokens = sem_tokens + text_tokens + ["<eos>"]
    return tokens


all_tokens = []
for text, sentiment, time, age, country in train_rows:
    all_tokens.extend(make_tokens(text, sentiment, time, age, country))


def build_vocab(tokens, max_vocab: int = 30000, min_freq: int = 1):
    from collections import Counter

    counter = Counter(tokens)
    # Reserve special tokens
    vocab = {"<pad>": 0, "<unk>": 1, "<bos>": 2, "<eos>": 3}

    # Remove already added specials
    for sp in list(vocab.keys()):
        if sp in counter:
            counter.pop(sp, None)

    items = [w for w, f in counter.items() if f >= min_freq]
    items.sort(key=lambda w: counter[w], reverse=True)
    items = items[: max_vocab - len(vocab)]

    for i, w in enumerate(items, start=len(vocab)):
        vocab[w] = i

    return vocab


vocab = build_vocab(all_tokens)
vocab_size = len(vocab)
pad_id = vocab["<pad>"]
bos_id = vocab["<bos>"]
eos_id = vocab["<eos>"]
max_len = 40


def encode_tokens(tokens):
    ids = [vocab.get(t, vocab["<unk>"]) for t in tokens][: max_len - 1]
    if len(ids) + 1 < max_len:
        ids.append(eos_id)
    ids = ids[: max_len - 1]
    ids = ids + [pad_id] * (max_len - len(ids))
    return ids


class Seq2SeqDataset(Dataset):
    def __init__(self, rows):
        self.inputs = []
        self.targets = []
        for text, sentiment, time, age, country in rows:
            tokens = make_tokens(text, sentiment, time, age, country)
            # input: all tokens except last
            inp_tokens = tokens[:-1]
            # target: all tokens except first
            tgt_tokens = tokens[1:]
            inp_ids = encode_tokens(inp_tokens)
            tgt_ids = encode_tokens(tgt_tokens)
            self.inputs.append(inp_ids)
            self.targets.append(tgt_ids)

    def __len__(self):
        return len(self.inputs)

    def __getitem__(self, idx):
        x = torch.tensor(self.inputs[idx], dtype=torch.long)
        y = torch.tensor(self.targets[idx], dtype=torch.long)
        return x, y


dataset = Seq2SeqDataset(train_rows)
train_dl = DataLoader(dataset, batch_size=128, shuffle=True)


class LSTMSeq2SeqLM(nn.Module):
    def __init__(self, vocab_size: int, emb_dim: int = 128, hidden_dim: int = 256, num_layers: int = 2):
        super().__init__()
        self.emb = nn.Embedding(vocab_size, emb_dim, padding_idx=pad_id)
        self.lstm = nn.LSTM(emb_dim, hidden_dim, num_layers=num_layers, batch_first=True)
        self.fc = nn.Linear(hidden_dim, vocab_size)

    def forward(self, x):
        emb = self.emb(x)
        out, _ = self.lstm(emb)
        logits = self.fc(out)
        return logits


device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device}")

model = LSTMSeq2SeqLM(vocab_size=vocab_size).to(device)
optimizer = torch.optim.Adam(model.parameters(), lr=2e-3)
criterion = nn.CrossEntropyLoss(ignore_index=pad_id)


def train_epoch(dl):
    model.train()
    total = 0
    loss_sum = 0.0
    for x, y in dl:
        x = x.to(device)
        y = y.to(device)
        optimizer.zero_grad()
        logits = model(x)  # [B, L, V]
        loss = criterion(logits.view(-1, vocab_size), y.view(-1))
        loss.backward()
        optimizer.step()
        loss_sum += loss.item() * y.size(0)
        total += y.size(0)
    return loss_sum / max(1, total)


for epoch in range(1, 5):
    loss = train_epoch(train_dl)
    print(f"[Seq2Seq-LSTM] epoch {epoch}: loss={loss:.4f}")


config = {
    "max_len": max_len,
    "pad_id": pad_id,
    "bos_id": bos_id,
    "eos_id": eos_id,
}

with (MODEL_DIR / "seq2seq_lstm_config.json").open("w", encoding="utf-8") as f:
    json.dump(config, f)

with (MODEL_DIR / "seq2seq_lstm_vocab.json").open("w", encoding="utf-8") as f:
    json.dump(vocab, f, ensure_ascii=False)


def generate_example(sentiment: str, time: str, age: str, country: str, max_new_tokens: int = 20) -> str:
    model.eval()
    # build semantic prefix tokens
    country_token = "country_" + re.sub(r"[^a-z]+", "_", country.lower()).strip("_")
    prefix_tokens = [
        "<bos>",
        f"sent_{sentiment.lower()}",
        f"time_{time.lower()}",
        f"age_{age}",
        country_token,
    ]
    ids = [vocab.get(t, vocab["<unk>"]) for t in prefix_tokens]

    for _ in range(max_new_tokens):
        x = torch.full((1, max_len), pad_id, dtype=torch.long, device=device)
        length = min(len(ids), max_len)
        x[0, :length] = torch.tensor(ids[:length], dtype=torch.long, device=device)
        with torch.no_grad():
            logits = model(x)  # [1, L, V]
        last_idx = min(length, max_len) - 1
        next_id = int(logits[0, last_idx].argmax(-1))
        if next_id in (pad_id, eos_id):
            break
        ids.append(next_id)

    # decode, skipping semantic prefix tokens
    inv_vocab = {i: t for t, i in vocab.items()}
    text_tokens = []
    for idx in ids[len(prefix_tokens) :]:
        tok = inv_vocab.get(idx, "")
        if not tok or tok in ("<pad>", "<eos>"):
            break
        if tok.startswith(("sent_", "time_", "age_", "country_")):
            continue
        text_tokens.append(tok)
    return " ".join(text_tokens) if text_tokens else "(no text generated)"


print("Sample generations (PyTorch, before ONNX export):")
for s, t, a, c in [
    ("positive", "morning", "0-20", "Afghanistan"),
    ("negative", "night", "31-45", "Algeria"),
    ("neutral", "noon", "21-30", "Albania"),
]:
    print(f"[{s}, {t}, {a}, {c}] -> {generate_example(s, t, a, c)}")


onnx_path = MODEL_DIR / "seq2seq_lstm.onnx"
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
    print(f"Exported seq2seq LSTM ONNX to {onnx_path}")
except Exception as e:
    print(f"WARNING: failed to export seq2seq LSTM to ONNX ({e}). The .pt model is still trained; try an older torch/onnx version if ONNX is required.")
