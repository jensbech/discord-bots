#!/usr/bin/env sh
set -e

IMAGE_NAME=${1:-"jensbech/bored-bot"}
PLATFORMS=${PLATFORMS:-"linux/amd64"}

GIT_COMMIT=$(git rev-parse --short HEAD)
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD)


LATEST_TAG=$(git tag --list 'v*' --sort=-v:refname | head -n1)

if [ -n "$LATEST_TAG" ]; then
    COMMITS_SINCE_TAG=$(git rev-list --count ${LATEST_TAG}..HEAD)
    
    if [ "$COMMITS_SINCE_TAG" -eq 0 ]; then
        VERSION="$LATEST_TAG"
    else
        BASE_VERSION=$(echo "$LATEST_TAG" | sed 's/^v//')
        MAJOR=$(echo "$BASE_VERSION" | cut -d. -f1)
        MINOR=$(echo "$BASE_VERSION" | cut -d. -f2)
        PATCH=$(echo "$BASE_VERSION" | cut -d. -f3)
        PATCH=$((PATCH + 1))
        VERSION="v${MAJOR}.${MINOR}.${PATCH}-dev.${COMMITS_SINCE_TAG}.${GIT_COMMIT}"
    fi
else
    COMMIT_COUNT=$(git rev-list --count HEAD)
    VERSION="v0.1.0-dev.${COMMIT_COUNT}.${GIT_COMMIT}"
fi

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