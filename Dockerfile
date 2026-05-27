# Этап 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копируем файл проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем весь код и публикуем релизную версию
COPY . ./
RUN dotnet publish -c Release -o out

# Этап 2: Запуск приложения
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out ./

# Устанавливаем порт (Railway передает переменную PORT)
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE ${PORT}

# Точка входа (замените MyWebApp.dll на имя вашего файла .dll)
ENTRYPOINT ["dotnet", "bot.dll"]