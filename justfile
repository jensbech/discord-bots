_default:
    @just --list

publish:
    chmod +x ./scripts/build_and_publish.sh
    ./scripts/build_and_publish.sh

run:
    cd DiscordBots.BoredBot && dotnet run

host:
    docker compose up

build:
    dotnet build

clean:
    dotnet clean
    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
