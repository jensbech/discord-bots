#!/usr/bin/env sh
set -e

tag=$(git tag --list 'v*' --sort=-v:refname | head -n1)
tag=${tag:-latest}

echo "Publishing with tag: $tag and latest"

IMAGE_NAME=${1:-"jensbech/bored-bot"}
PLATFORMS=${PLATFORMS:-"linux/amd64"}

build_args=""
if [ -n "$DOCKER_BUILD_ARGS" ]; then
    for kv in $DOCKER_BUILD_ARGS; do
        build_args="$build_args --build-arg $kv"
    done
fi

if [ "$tag" = "latest" ]; then
    other_tag_flags=""
else
    other_tag_flags="--tag $IMAGE_NAME:$tag"
fi

echo "Building image $IMAGE_NAME with tag $tag (always tagging latest)"
echo "Using platforms: $PLATFORMS"

docker buildx build \
    --platform "$PLATFORMS" \
    --tag "$IMAGE_NAME:latest" \
    $other_tag_flags \
    $build_args \
    --file DiscordBots.BoredBot/Dockerfile \
    --push \
    .

echo "Successfully published $IMAGE_NAME:latest"
if [ "$tag" != "latest" ]; then
    echo "Successfully published $IMAGE_NAME:$tag"
fi