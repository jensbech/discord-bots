_default:
    @just --list

publish IMAGE='jensbech/bored-bot':
    chmod +x ./scripts/build_and_publish.sh
    ./scripts/build_and_publish.sh {{IMAGE}}

run:
    cd DiscordBots.BoredBot && dotnet run

build:
    dotnet build

clean:
    dotnet clean
    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
