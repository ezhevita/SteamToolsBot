﻿FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SteamToolsBot/SteamToolsBot.csproj", "SteamToolsBot/"]
RUN dotnet restore "SteamToolsBot/SteamToolsBot.csproj"
COPY . .
WORKDIR "/src/SteamToolsBot"
RUN dotnet build "SteamToolsBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SteamToolsBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamToolsBot.dll"]
