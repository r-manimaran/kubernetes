# Set variables
$ACR_NAME = "maranacr"
$IMAGE_NAME = "azfunimageuploader"
$VERSION = "v1"

# Build the image
Write-Host "Building image..."
docker build -t "$IMAGE_NAME`:$VERSION" .

# Tag images
Write-Host "Tagging images.."
docker tag "$IMAGE_NAME`:$VERSION" "$ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION"
docker tag "$IMAGE_NAME`:$VERSION" "$ACR_NAME.azurecr.io/$IMAGE_NAME`:latest"

# Login to ACR
Write-Host "Logging into ACR..."
az acr login --name $ACR_NAME

# Push images
Write-Host "Pushing images..."
docker push "$ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION"
docker push "$ACR_NAME.azurecr.io/$IMAGE_NAME`:latest"

Write-Host "Cleaning up local images..."
docker rmi "$ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION"
docker rmi "$ACR_NAME.azurecr.io/$IMAGE_NAME`:latest"
docker rmi "$IMAGE_NAME`:$VERSION"

Write-Host "Build and push completed successfully."