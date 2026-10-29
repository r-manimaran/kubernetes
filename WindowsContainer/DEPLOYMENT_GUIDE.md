# Windows Container Deployment Guide
## .NET Framework Web API — Local Docker Desktop & AKS

---

## Prerequisites

| Tool | Purpose |
|------|---------|
| Docker Desktop (Windows) | Local container build & run |
| Kubernetes enabled in Docker Desktop | Local K8s cluster |
| kubectl | Interact with K8s clusters |
| Azure CLI | Provision and manage AKS |
| An Azure Subscription | Host AKS cluster |
| Azure Container Registry (ACR) | Store container images |

---

## Important: Windows Container Limitation in Docker Desktop

Docker Desktop can run **either** Linux containers **or** Windows containers at a time — not both simultaneously.

> Right-click the Docker Desktop tray icon → **Switch to Windows containers...**

Windows containers on Docker Desktop use **process isolation** by default.  
The host OS build version must be **compatible** with the container image OS version.

Check your Windows build:
```powershell
winver
# or
[System.Environment]::OSVersion.Version
```

The project Dockerfile uses `windowsservercore-ltsc2019` (OS build **10.0.17763**).  
If your host is Windows 11 or Windows Server 2022, you must use **Hyper-V isolation** or switch the base image to `ltsc2022`.

---

## Part 1 — Build & Run Locally with Docker Desktop

### Step 1 — Switch Docker Desktop to Windows Containers

Right-click the Docker Desktop system tray icon and select **Switch to Windows containers**.  
Confirm when prompted. Docker Desktop will restart briefly.

Verify:
```powershell
docker version
# Server OS/Arch should show: windows/amd64
```

---

### Step 2 — Update the Dockerfile

The existing Dockerfile uses a `source` ARG pattern meant for Visual Studio publish.  
Replace it with a self-contained build that works from the command line.

Update `AlgorithmApi/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
WORKDIR /app

COPY AlgorithmApi/packages.config ./
COPY AlgorithmApi/ ./

RUN nuget restore AlgorithmApi.csproj -PackagesDirectory ../packages
RUN msbuild AlgorithmApi.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:PublishUrl=publish\

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
WORKDIR /inetpub/wwwroot
COPY --from=build /app/publish .
```

> If you prefer to publish from Visual Studio first, see the **Visual Studio Publish approach** below.

#### Visual Studio Publish Approach (simpler for local testing)

1. Right-click the project in Solution Explorer → **Publish**
2. Choose **Folder** → publish to `AlgorithmApi\obj\Docker\publish`
3. Use this simpler Dockerfile (already in the project):

```dockerfile
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
WORKDIR /inetpub/wwwroot
COPY obj/Docker/publish .
```

Then build from inside the `AlgorithmApi` folder:
```powershell
cd AlgorithmApi
docker build -t algorithmapi:local .
```

---

### Step 3 — Build the Docker Image

From the solution root (using the multi-stage Dockerfile):
```powershell
docker build -t algorithmapi:local -f AlgorithmApi/Dockerfile .
```

This will take several minutes the first time — the Windows Server Core base image is ~5 GB.

Verify the image was created:
```powershell
docker images | findstr algorithmapi
```

---

### Step 4 — Run the Container Locally

```powershell
docker run -d -p 8080:80 --name algorithmapi algorithmapi:local
```

Test the endpoints:
```powershell
# Swagger UI
start http://localhost:8080/swagger

# Test an endpoint
curl -X GET http://localhost:8080/api/algorithm/palindrome/racecar

curl -X POST http://localhost:8080/api/algorithm/sort `
  -H "Content-Type: application/json" `
  -d '{"numbers":[5,3,1,4,2]}'
```

Stop and remove the container:
```powershell
docker stop algorithmapi
docker rm algorithmapi
```

---

### Step 5 — Test in Local Kubernetes (Docker Desktop)

#### 5.1 — Enable Kubernetes in Docker Desktop

Open Docker Desktop → **Settings** → **Kubernetes** → check **Enable Kubernetes** → **Apply & Restart**.

Verify:
```powershell
kubectl config current-context
# Should output: docker-desktop

kubectl get nodes
# Should show a single node with STATUS: Ready
```

> **Note:** Docker Desktop Kubernetes runs Linux nodes only. Windows container workloads **cannot** be scheduled on it directly. You have two options:
> - Use `docker run` (Step 4) for local Windows container testing
> - Push to ACR and test on AKS with a Windows node pool (Part 2)
>
> This is a known Docker Desktop limitation — it does not support Windows nodes in its embedded Kubernetes cluster.

---

## Part 2 — Deploy to AKS as Windows Container

### Step 1 — Login to Azure

```powershell
az login
az account set --subscription "<your-subscription-id>"
```

---

### Step 2 — Create a Resource Group

```powershell
az group create --name rg-windows-containers --location eastus
```

---

### Step 3 — Create Azure Container Registry (ACR)

```powershell
az acr create `
  --resource-group rg-windows-containers `
  --name <your-acr-name> `
  --sku Basic

# Login to ACR
az acr login --name <your-acr-name>
```

---

### Step 4 — Tag and Push the Image to ACR

```powershell
# Tag the local image
docker tag algorithmapi:local <your-acr-name>.azurecr.io/algorithmapi:v1

# Push to ACR
docker push <your-acr-name>.azurecr.io/algorithmapi:v1

# Verify
az acr repository list --name <your-acr-name> --output table
```

---

### Step 5 — Create AKS Cluster with a Windows Node Pool

AKS requires a Linux system node pool first, then a Windows node pool is added separately.

```powershell
# Create AKS cluster with Linux system node pool
az aks create `
  --resource-group rg-windows-containers `
  --name aks-windows-demo `
  --node-count 1 `
  --node-vm-size Standard_D2s_v3 `
  --network-plugin azure `
  --generate-ssh-keys `
  --attach-acr <your-acr-name>

# Add a Windows node pool
az aks nodepool add `
  --resource-group rg-windows-containers `
  --cluster-name aks-windows-demo `
  --os-type Windows `
  --name winnp `
  --node-count 1 `
  --node-vm-size Standard_D2s_v3
```

> The Windows node pool VM runs **Windows Server 2019** which is compatible with the `ltsc2019` base image.

---

### Step 6 — Get AKS Credentials

```powershell
az aks get-credentials `
  --resource-group rg-windows-containers `
  --name aks-windows-demo

# Verify nodes — you should see both Linux and Windows nodes
kubectl get nodes -o wide
```

---

### Step 7 — Create Kubernetes Manifests

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: algorithmapi
spec:
  replicas: 1
  selector:
    matchLabels:
      app: algorithmapi
  template:
    metadata:
      labels:
        app: algorithmapi
    spec:
      nodeSelector:
        kubernetes.io/os: windows
      containers:
        - name: algorithmapi
          image: <your-acr-name>.azurecr.io/algorithmapi:v1
          ports:
            - containerPort: 80
          resources:
            requests:
              cpu: "250m"
              memory: "512Mi"
            limits:
              cpu: "500m"
              memory: "1Gi"
---
apiVersion: v1
kind: Service
metadata:
  name: algorithmapi-svc
spec:
  type: LoadBalancer
  selector:
    app: algorithmapi
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
```

> The `nodeSelector: kubernetes.io/os: windows` is critical — it ensures the pod is scheduled on the Windows node pool.

---

### Step 8 — Deploy to AKS

```powershell
kubectl apply -f k8s/deployment.yaml

# Watch the pod come up (Windows containers take ~2-3 minutes to start)
kubectl get pods -w

# Check deployment status
kubectl get deployment algorithmapi
```

---

### Step 9 — Get the External IP and Test

```powershell
# Wait for the LoadBalancer to assign an external IP
kubectl get svc algorithmapi-svc -w

# Once EXTERNAL-IP is assigned (not <pending>):
$ip = kubectl get svc algorithmapi-svc -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Test
curl http://$ip/swagger
curl http://$ip/api/algorithm/palindrome/racecar
```

---

## Troubleshooting

### Pod stuck in `ContainerCreating`
Windows container images are large (~5 GB). The node needs time to pull the image on first deploy.
```powershell
kubectl describe pod <pod-name>
# Look at Events section for pull progress
```

### `ImagePullBackOff`
ACR is not attached to AKS or credentials are stale.
```powershell
az aks update --name aks-windows-demo --resource-group rg-windows-containers --attach-acr <your-acr-name>
```

### OS version mismatch error in container
If you see `The container operating system does not match the host operating system`, the Windows node pool OS version doesn't match the base image tag. Switch the Dockerfile base image to match:
- Windows Server 2022 nodes → use `ltsc2022`
- Windows Server 2019 nodes → use `ltsc2019`

### Check container logs
```powershell
kubectl logs <pod-name>
kubectl exec -it <pod-name> -- powershell
```

---

## Cleanup

```powershell
# Delete AKS cluster
az aks delete --name aks-windows-demo --resource-group rg-windows-containers --yes --no-wait

# Delete resource group (removes everything)
az group delete --name rg-windows-containers --yes --no-wait
```

---

## Summary

```
Local Testing Flow:
  Docker Desktop (Windows containers mode)
    └── docker build → docker run → test on localhost:8080

AKS Deployment Flow:
  docker build
    └── docker push → ACR
          └── kubectl apply → AKS Windows Node Pool
                └── LoadBalancer Service → External IP
```
