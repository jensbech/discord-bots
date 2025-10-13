FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for dependency resolution
COPY DiscordBots.Core/DiscordBots.Core.csproj DiscordBots.Core/
COPY DiscordBots.BookStack/DiscordBots.BookStack.csproj DiscordBots.BookStack/
COPY DiscordBots.OpenAI/DiscordBots.OpenAI.csproj DiscordBots.OpenAI/
COPY DiscordBots.Scribe/DiscordBots.Scribe.csproj DiscordBots.Scribe/

# Restore dependencies
RUN dotnet restore DiscordBots.Scribe/DiscordBots.Scribe.csproj

# Copy all source code
COPY . .

# Build and publish the application
RUN dotnet publish DiscordBots.Scribe/DiscordBots.Scribe.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DiscordBots.Scribe.dll"]