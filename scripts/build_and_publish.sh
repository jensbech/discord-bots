#!/usr/bin/env sh
set -e

MAJOR_VERSION="1"

IMAGE_NAME=${1:-"git.bechsor.no/jens/discord-bots"}
PLATFORMS=${PLATFORMS:-"linux/amd64"}



REPO_NAME=$(echo "$IMAGE_NAME" | sed 's|.*/||')
NAMESPACE=$(echo "$IMAGE_NAME" | cut -d/ -f2)

VERSION="v${MAJOR_VERSION}.0.0"
if docker manifest inspect "$IMAGE_NAME:latest" >/dev/null 2>&1; then
    VERSION="v${MAJOR_VERSION}.$(date +%Y%m%d%H%M%S).0"
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
    --file Dockerfile \
    --output type=registry \
    .

echo ""
echo "Published:"
echo "   $IMAGE_NAME:latest"
echo "   $IMAGE_NAME:$VERSION"