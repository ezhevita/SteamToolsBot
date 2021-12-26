FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TelegramSteamKeysChecker/TelegramSteamKeysChecker.csproj", "TelegramSteamKeysChecker/"]
RUN dotnet restore "TelegramSteamKeysChecker/TelegramSteamKeysChecker.csproj"
COPY . .
WORKDIR "/src/TelegramSteamKeysChecker"
RUN dotnet build "TelegramSteamKeysChecker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TelegramSteamKeysChecker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["TelegramSteamKeysChecker/config.json", "."]
COPY ["TelegramSteamKeysChecker/banned.json", "."]
ENTRYPOINT ["dotnet", "TelegramSteamKeysChecker.dll"]
