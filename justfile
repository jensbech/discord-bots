_default:
    @just --list

bored-dev:
    cd DiscordBots.BoredBot && dotnet run

build:
    dotnet build