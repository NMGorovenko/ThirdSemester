FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Копируем проект и восстанавливаем зависимости
COPY Lab6/src/Lab6.TelegramBot.csproj Lab6/src/
RUN dotnet restore Lab6/src/Lab6.TelegramBot.csproj

# Копируем оставшийся исходный код и публикуем
COPY Lab6/src/ Lab6/src/
RUN dotnet publish Lab6/src/Lab6.TelegramBot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

# Среда выполнения: все секреты приходят через переменные окружения
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Lab6.TelegramBot.dll"]

