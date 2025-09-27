_default:
    @just --list

publish IMAGE='your-docker-user/discord-bored-bot':
    chmod +x ./scripts/build_and_publish.sh
    ./scripts/build_and_publish.sh {{IMAGE}}

bored-dev:
    cd DiscordBots.BoredBot && dotnet run

build:
    dotnet build