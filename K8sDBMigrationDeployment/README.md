# Apply Migration when running the App in Kubernetes

![Update Database](image-2.png)

![alt text](image.png)

![alt text](image-1.png)

![alt text](image-3.png)

## Build the Docker Image with tag and Push to Docker Hub
```bash
docker build -t <dockerhub-username>/dotnet-migration:latest .
docker push <dockerhub-username>/dotnet-migration:latest
```

## Create a Kubernetes Deployment and Service
```yaml
