#!/bin/bash

# Set variables
ACR_NAME="maranacr"
IMAGE_NAME="processingapp"
VERSION="v1"

# Build the image
docker build -t $IMAGE_NAME:$VERSION .

# Tag images
echo "Tagging images..."
docker tag $IMAGE_NAME:$VERSION $ACR_NAME.azurecr.io/$IMAGE_NAME:$VERSION
docker tag $IMAGE_NAME:$VERSION $ACR_NAME.azurecr.io/$IMAGE_NAME:latest

# Login to ACR
echo "Logging in to ACR..."
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" --output tsv)
echo $ACR_PASSWORD | docker login $ACR_NAME.azurecr.io --username $ACR_NAME --password-stdin

# Push images
echo "Pushing images..."
docker push $ACR_NAME.azurecr.io/$IMAGE_NAME:$VERSION
docker push $ACR_NAME.azurecr.io/$IMAGE_NAME:latest

# clean up
echo "Cleaning up local images"
docker rmi $ACR_NAME.azurecr.io/$IMAGE_NAME:$VERSION
docker rmi $ACR_NAME.azurecr.io/$IMAGE_NAME:latest
docker rmi $IMAGE_NAME:$VERSION

echo "Process completed successfully"