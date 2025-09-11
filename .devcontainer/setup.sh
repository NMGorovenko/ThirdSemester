#!/bin/bash
set -e

# Python пакеты
pip install --upgrade pip
pip install jupyterlab nbconvert

# Устанавливаем .NET Interactive
dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive
/home/vscode/.dotnet/tools/dotnet-interactive jupyter install --default-kernel csharp
