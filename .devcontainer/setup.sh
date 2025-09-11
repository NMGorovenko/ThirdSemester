#!/bin/bash
set -euo pipefail

echo "[setup] Ensuring user local bin in PATH"
export PATH="$HOME/.local/bin:$PATH"
if ! grep -q 'HOME/.local/bin' "$HOME/.bashrc" 2>/dev/null; then
  echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bashrc"
fi

echo "[setup] Upgrading pip and installing Jupyter + nbconvert"
python -m pip install --upgrade pip --no-input
python -m pip install --no-input --user jupyterlab nbconvert

echo "[setup] Verifying installations"
python -c "import nbconvert, sys; print('[setup] nbconvert', nbconvert.__version__)"
command -v jupyter && jupyter --version || echo "[setup] jupyter not found in PATH (will still work via python -m jupyter)"

echo "[setup] Installing .NET Interactive"
dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive
/home/vscode/.dotnet/tools/dotnet-interactive jupyter install --default-kernel csharp || true
echo "[setup] Completed"
