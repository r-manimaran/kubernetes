#!/bin/bash

# Variables
DOCKER_USERNAME="rmanimaran"
IMAGE_NAME="dotnetk8s"
TAG="latest"

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "Docker does not seem to be running. Please start Docker first."
    exit 1
fi

# Check if user is logged into Docker Hub
if ! docker info | grep -q "Username"; then
    echo "Please log in to Docker Hub first using: docker login"
    exit 1
fi

echo "Building Docker image..."
# Build the Docker image
if docker build -t $IMAGE_NAME .; then
    echo "Docker image built successfully"
else
    echo "Error building Docker image"
    exit 1
fi

echo "Tagging Docker image..."
# Tag the image
if docker tag $IMAGE_NAME $DOCKER_USERNAME/$IMAGE_NAME:$TAG; then
    echo "Docker image tagged successfully"
else
    echo "Error tagging Docker image"
    exit 1
fi

echo "Pushing Docker image to Docker Hub..."
# Push the image to Docker Hub
if docker push $DOCKER_USERNAME/$IMAGE_NAME:$TAG; then
    echo "Docker image pushed successfully"
else
    echo "Error pushing Docker image"
    exit 1
fi

echo "Process completed successfully!"

## chmod +x build_and_push.sh
## ./build_and_push.sh
