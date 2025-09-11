FROM mcr.microsoft.com/devcontainers/dotnet:9.0

# Install Python and pip, then Jupyter + nbconvert globally for all users
USER root
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
       python3 python3-pip python3-venv \
    && rm -rf /var/lib/apt/lists/* \
    && python3 -m pip install --no-cache-dir --upgrade pip \
    && python3 -m pip install --no-cache-dir jupyterlab nbconvert

# Install .NET Interactive for the vscode user and register Jupyter kernel
ARG USERNAME=vscode
RUN su - ${USERNAME} -c "dotnet tool install -g Microsoft.dotnet-interactive || dotnet tool update -g Microsoft.dotnet-interactive" \
    && su - ${USERNAME} -c "~/.dotnet/tools/dotnet-interactive jupyter install --default-kernel csharp || true"

# Back to root so devcontainer Features (if any) can install cleanly
USER root

