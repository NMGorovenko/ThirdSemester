#!/bin/bash
set -e

# Устанавливаем Git LFS
sudo apt-get update
curl -s https://packagecloud.io/install/repositories/github/git-lfs/script.deb.sh | sudo bash
sudo apt-get install -y git-lfs

# Настройка Python
pip install --upgrade pip
pip install jupyterlab nbconvert

# Устанавливаем .NET Interactive
dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive
/home/vscode/.dotnet/tools/dotnet-interactive jupyter install --default-kernel csharp
