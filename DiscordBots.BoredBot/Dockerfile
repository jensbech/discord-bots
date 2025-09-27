FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY DiscordBots.Core/DiscordBots.Core.csproj DiscordBots.Core/
COPY DiscordBots.BoredBot/DiscordBots.BoredBot.csproj DiscordBots.BoredBot/

RUN dotnet restore DiscordBots.BoredBot/DiscordBots.BoredBot.csproj

COPY . .

RUN dotnet publish DiscordBots.BoredBot/DiscordBots.BoredBot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DiscordBots.BoredBot.dll"]
