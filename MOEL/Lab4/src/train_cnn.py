import os, json, re, math, random
from pathlib import Path

import torch
import torch.nn as nn
from torch.utils.data import Dataset, DataLoader

DATA_DIR = Path(__file__).resolve().parents[1] / 'data'
MODEL_DIR = Path(__file__).resolve().parents[1] / 'models'
MODEL_DIR.mkdir(parents=True, exist_ok=True)

TRAIN_CSV = DATA_DIR / 'train.csv'
TEST_CSV  = DATA_DIR / 'test.csv'

def normalize(s: str) -> str:
    s = s.lower()
    s = re.sub(r'https?://\S+', ' ', s)
    # удалить кавычки и апострофы
    s = re.sub(r'["\'`]+', '', s)
    # оставить только латинские буквы/цифры, #, @ и пробелы
    s = re.sub(r'[^0-9a-z#@ ]+', ' ', s)
    s = re.sub(r'\s+', ' ', s).strip()
    return s

def read_csv_train(path: Path):
    import csv
    rows = []
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        r = csv.DictReader(f)
        for row in r:
            t = row.get('text') or ''
            y = (row.get('sentiment') or '').strip()
            if not t or not y:
                continue
            rows.append((normalize(t), y))
    return rows

def read_csv_test(path: Path):
    import csv
    rows = []
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        r = csv.DictReader(f)
        for row in r:
            t = row.get('text') or ''
            y = (row.get('sentiment') or '').strip()
            if not t or not y:
                continue
            rows.append((normalize(t), y))
    return rows

train = read_csv_train(TRAIN_CSV)
test  = read_csv_test(TEST_CSV)
print(f'Train={len(train)} Test={len(test)}')

labels = {'negative':0,'neutral':1,'positive':2}

def build_vocab(texts, max_vocab=20000, min_freq=2):
    from collections import Counter
    c = Counter()
    for s in texts:
        c.update(s.split())
    it = [w for w,f in c.items() if f>=min_freq]
    it.sort(key=lambda w: c[w], reverse=True)
    it = it[:max_vocab-2]
    idx = {'<pad>':0,'<unk>':1}
    for i,w in enumerate(it, start=2):
        idx[w]=i
    return idx

vocab = build_vocab([t for t,_ in train])
max_len = 40

def to_ids(s: str):
    arr = [vocab.get(t,1) for t in s.split()][:max_len]
    if len(arr)<max_len:
        arr += [0]*(max_len-len(arr))
    return arr

class DS(Dataset):
    def __init__(self, rows):
        self.X = [to_ids(t) for t,_ in rows]
        self.y = [labels[y] for _,y in rows]
    def __len__(self): return len(self.X)
    def __getitem__(self, i):
        return torch.tensor(self.X[i], dtype=torch.long), torch.tensor(self.y[i], dtype=torch.long)

train_ds = DS(train)
test_ds  = DS(test)
train_dl = DataLoader(train_ds, batch_size=128, shuffle=True)
test_dl  = DataLoader(test_ds, batch_size=256)

class CNN(nn.Module):
    def __init__(self, vocab_size, emb_dim=100, num_filters=64, kernels=(3,4,5), num_classes=3):
        super().__init__()
        self.emb = nn.Embedding(vocab_size, emb_dim)
        self.convs = nn.ModuleList([nn.Conv1d(emb_dim, num_filters, k) for k in kernels])
        self.fc = nn.Linear(num_filters*len(kernels), num_classes)
        self.dp = nn.Dropout(0.5)
    def forward(self, x):
        x = self.emb(x)              # [B,L,D]
        x = x.transpose(1,2)         # [B,D,L]
        outs = [torch.max(torch.relu(c(x)), dim=2).values for c in self.convs]
        x = torch.cat(outs, dim=1)   # [B, F*K]
        x = self.dp(x)
        return self.fc(x)

device = 'cuda' if torch.cuda.is_available() else 'cpu'
model = CNN(len(vocab)).to(device)
opt = torch.optim.Adam(model.parameters(), lr=2e-3)
crit = nn.CrossEntropyLoss()

def run_epoch(dl, train=True):
    if train: model.train();
    else: model.eval()
    total=0; correct=0; loss_sum=0.0
    for X,y in dl:
        X=X.to(device); y=y.to(device)
        if train: opt.zero_grad()
        logits = model(X)
        loss = crit(logits, y)
        if train:
            loss.backward(); opt.step()
        loss_sum += loss.item()*y.size(0)
        pred = logits.argmax(1)
        correct += (pred==y).sum().item()
        total += y.size(0)
    return loss_sum/max(1,total), correct/max(1,total)

for ep in range(1,3):
    trL,trA = run_epoch(train_dl, True)
    teL,teA = run_epoch(test_dl, False)
    print(f'[CNN] epoch {ep}: train loss={trL:.4f}, acc={trA:.3f} | test loss={teL:.4f}, acc={teA:.3f}')

# Export ONNX
onnx_path = MODEL_DIR / 'cnn_text.onnx'
config = {'max_len': max_len}
with open(MODEL_DIR / 'config.json','w') as f:
    json.dump(config,f)
with open(MODEL_DIR / 'vocab.json','w') as f:
    json.dump(vocab,f, ensure_ascii=False)

dummy = torch.randint(0, len(vocab), (1,max_len), dtype=torch.long, device=device)
torch.onnx.export(
    model, dummy, onnx_path.as_posix(),
    input_names=['input_ids'], output_names=['logits'],
    dynamic_axes={'input_ids':{0:'batch'}, 'logits':{0:'batch'}},
    opset_version=13
)
print(f'Exported ONNX to {onnx_path}')
