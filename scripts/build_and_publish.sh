#!/usr/bin/env sh
set -e

MAJOR_VERSION="1"

IMAGE_NAME=${1:-"jensbech/bored-bot"}
PLATFORMS=${PLATFORMS:-"linux/amd64"}


REPO_NAME=$(echo "$IMAGE_NAME" | sed 's|.*\/||')
NAMESPACE=$(echo "$IMAGE_NAME" | sed 's|\/.*||')

CURRENT_VERSION=$(curl -s "https://hub.docker.com/v2/repositories/${NAMESPACE}/${REPO_NAME}/tags/?page_size=100" | \
    grep -o '"name":"v'${MAJOR_VERSION}'\.[0-9]\+\.[0-9]\+"' | \
    sed 's/"name":"//;s/"//' | \
    sort -V | tail -n1 || echo "")

if [ -z "$CURRENT_VERSION" ]; then
    echo "   No previous v${MAJOR_VERSION}.x.x versions found, starting with v${MAJOR_VERSION}.0.0"
    VERSION="v${MAJOR_VERSION}.0.0"
else
    echo "   Current latest v${MAJOR_VERSION}.x.x version: $CURRENT_VERSION"
    BASE_VERSION=$(echo "$CURRENT_VERSION" | sed 's/^v//')
    MINOR=$(echo "$BASE_VERSION" | cut -d. -f2)

    MINOR=$((MINOR + 1))
    VERSION="v${MAJOR_VERSION}.${MINOR}.0"
    echo "   New version: $VERSION"
fi

GIT_COMMIT=$(git rev-parse --short HEAD)
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)

echo "Building and publishing:"
echo "  Image: $IMAGE_NAME"
echo "  Version: $VERSION"
echo "  Git commit: $GIT_COMMIT"
echo "  Branch: $GIT_BRANCH"
echo "  Platforms: $PLATFORMS"

build_args=""
if [ -n "$DOCKER_BUILD_ARGS" ]; then
    for kv in $DOCKER_BUILD_ARGS; do
        build_args="$build_args --build-arg $kv"
    done
fi

docker buildx build \
    --platform "$PLATFORMS" \
    --tag "$IMAGE_NAME:latest" \
    --tag "$IMAGE_NAME:$VERSION" \
    $build_args \
    --file DiscordBots.BoredBot/Dockerfile \
    --push \
    .

echo ""
echo "✅ Successfully published:"
echo "   $IMAGE_NAME:latest"
echo "   $IMAGE_NAME:$VERSION"