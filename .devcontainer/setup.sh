#!/bin/bash
set -e

# Базовые пакеты
sudo apt-get update
sudo apt-get install -y git-lfs

# Настройка Git LFS
git lfs install
git lfs pull

# Python пакеты
pip install --upgrade pip
pip install jupyterlab nbconvert

# .NET Interactive
dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive
/home/vscode/.dotnet/tools/dotnet-interactive jupyter install --default-kernel csharp
