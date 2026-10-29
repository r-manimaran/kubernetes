# AKS Windows Container Deployment Guide
## .NET Framework Web API — Minimum Cost Setup

---

## Cost Strategy

| Decision | Choice | Reason |
|----------|--------|--------|
| ACR SKU | Basic | Cheapest tier, sufficient for testing |
| AKS Linux system node | Standard_B2s | Burstable, lowest cost for system workloads |
| AKS Windows node | Standard_D2s_v3 | Minimum size that supports Windows containers |
| Node count | 1 each | Bare minimum for testing |
| Region | eastus | Generally lower pricing |
| Kubernetes version | Default (latest stable) | No extra cost |
| Stop cluster when idle | Yes | Avoid charges when not testing |

> **Estimated cost:** ~$0.10–$0.15/hr while running. Stop the node pools when not in use to reduce to near $0 (only ACR storage remains, ~$0.003/day for Basic).

---

## Prerequisites

Install the following tools before starting:

```powershell
# Verify Azure CLI
az --version

# Verify kubectl
kubectl version --client

# Verify Docker Desktop (must be in Windows containers mode)
docker version
```

If Azure CLI is not installed:
```powershell
winget install Microsoft.AzureCLI
```

---

## Part 1 — Prepare the Docker Image

> No Docker Desktop, no Visual Studio Publish step needed.
> ACR handles the full restore, compile, and package using the multi-stage Dockerfile.

### Step 1 — Verify the Dockerfile

The multi-stage Dockerfile at `AlgorithmApi/Dockerfile` is already updated and contains:

```dockerfile
# Stage 1 — Restore and Build
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
WORKDIR /src

COPY packages/ ./packages/
COPY AlgorithmApi/ ./AlgorithmApi/

WORKDIR /src/AlgorithmApi
RUN nuget restore AlgorithmApi.csproj -PackagesDirectory ../packages
RUN msbuild AlgorithmApi.csproj /p:Configuration=Release /p:DeployOnBuild=true /p:PublishUrl=C:\publish

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
WORKDIR /inetpub/wwwroot
COPY --from=build C:\publish .
```

**What each stage does:**
- Stage 1 uses the full .NET Framework SDK image — restores NuGet packages and runs MSBuild to compile and publish the app into `C:\publish`
- Stage 2 uses the lightweight ASP.NET runtime image — copies only the published output, keeping the final image as small as possible

---

### Step 2 — Verify Azure CLI is Installed

```powershell
az --version
# Minimum required: azure-cli 2.40.0
```

If not installed:
```powershell
winget install Microsoft.AzureCLI
```

---

## Part 2 — Azure Setup

### Step 4 — Login to Azure

```powershell
az login

# List subscriptions and pick the right one
az account list --output table

az account set --subscription "<your-subscription-id>"
```

---

### Step 5 — Create Resource Group

```powershell
az group create `
  --name rg-wincontainer-test `
  --location eastus
```

---

### Step 6 — Create Azure Container Registry (Basic SKU)

```powershell
# ACR name must be globally unique, alphanumeric only
az acr create `
  --resource-group rg-wincontainer-test `
  --name <your-acr-name> `
  --sku Basic `
  --admin-enabled true

# Login to ACR
az acr login --name <your-acr-name>
```

> Basic ACR costs ~$0.167/day. Delete it after testing to stop charges.

---

### Step 7 — Build and Push Image to ACR

Run this from the **solution root** (`WindowsContainer` folder). The `.` uploads the entire folder as the build context to ACR.

```powershell
# Make sure you are in the solution root
cd c:\Maran\Study\Git\Dotnet\kubernetes\WindowsContainer

az acr build `
  --registry <your-acr-name> `
  --image algorithmapi:v1 `
  --platform windows `
  --file AlgorithmApi/Dockerfile `
  .
```

**What happens when you run this:**
1. Azure CLI zips the solution folder and uploads it to ACR as the build context
2. ACR spins up a Windows build agent in Azure
3. Stage 1 runs on the agent — restores NuGet packages and compiles with MSBuild
4. Stage 2 copies only the published output into the runtime image
5. The final image is pushed directly into your ACR
6. Build logs stream back to your terminal in real time

> This will take **5–10 minutes** on first run — the Windows SDK base image is large. Subsequent builds are faster due to layer caching.

Verify the image is in ACR:
```powershell
az acr repository list --name <your-acr-name> --output table
az acr repository show-tags --name <your-acr-name> --repository algorithmapi --output table
```

---

## Part 3 — AKS Cluster Setup

### Step 8 — Create AKS Cluster with Linux System Node Pool

AKS always requires a Linux node pool for system components (kube-dns, metrics, etc.).  
We use the smallest burstable VM to keep costs low.

```powershell
az aks create `
  --resource-group rg-wincontainer-test `
  --name aks-wincontainer-test `
  --node-count 1 `
  --node-vm-size Standard_B2s `
  --network-plugin azure `
  --generate-ssh-keys `
  --attach-acr <your-acr-name> `
  --nodepool-name systempool `
  --nodepool-labels nodepool=system
```

> This takes 5–10 minutes. The `--attach-acr` flag grants AKS permission to pull images from your ACR automatically.

---

### Step 9 — Add Windows Node Pool

```powershell
az aks nodepool add `
  --resource-group rg-wincontainer-test `
  --cluster-name aks-wincontainer-test `
  --os-type Windows `
  --name winnp `
  --node-count 1 `
  --node-vm-size Standard_D2s_v3
```

> `Standard_D2s_v3` (2 vCPU, 8 GB RAM) is the minimum practical size for Windows Server containers. Smaller VMs often fail due to Windows OS overhead.

Verify both node pools are ready:
```powershell
az aks get-credentials `
  --resource-group rg-wincontainer-test `
  --name aks-wincontainer-test

kubectl get nodes -o wide
# Expect: 1 Linux node (systempool) + 1 Windows node (winnp)
```

---

## Part 4 — Kubernetes Manifests

### Step 10 — Create the k8s Folder

```powershell
mkdir k8s
```

---

### Step 11 — Create deployment.yaml

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: algorithmapi
  labels:
    app: algorithmapi
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
```

> `nodeSelector: kubernetes.io/os: windows` is mandatory — without it the pod will be scheduled on the Linux node and fail immediately.

---

### Step 12 — Create service.yaml

Create `k8s/service.yaml`:

```yaml
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

> A `LoadBalancer` service provisions an Azure Public Load Balancer. For cost savings after testing, delete the service — the Load Balancer has an hourly charge (~$0.025/hr).

---

## Part 5 — Deploy and Test

### Step 13 — Apply the Manifests

```powershell
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

---

### Step 14 — Monitor the Deployment

Windows containers take **2–5 minutes** to start on first pull due to the large base image.

```powershell
# Watch pod status
kubectl get pods -w

# Expected progression:
# Pending → ContainerCreating → Running

# If stuck in ContainerCreating, check events
kubectl describe pod <pod-name>
# Look for: "Pulling image" in Events — this is normal, just wait
```

---

### Step 15 — Get the External IP

```powershell
# Watch until EXTERNAL-IP is no longer <pending>
kubectl get svc algorithmapi-svc -w
```

Once assigned:
```powershell
$ip = kubectl get svc algorithmapi-svc -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
Write-Host "App URL: http://$ip"
```

---

### Step 16 — Test the API

```powershell
# Swagger UI — open in browser
start "http://$ip/swagger"

# Palindrome check
curl http://$ip/api/algorithm/palindrome/racecar

# Sort
curl -X POST "http://$ip/api/algorithm/sort" `
  -H "Content-Type: application/json" `
  -d '{"numbers":[5,3,1,4,2]}'

# Binary search
curl -X POST "http://$ip/api/algorithm/search" `
  -H "Content-Type: application/json" `
  -d '{"numbers":[5,3,1,4,2],"target":3}'
```

---

## Part 6 — Cost Control: Stop When Not Testing

### Stop the Windows Node Pool (saves ~$0.10/hr)

When you are done testing for the day, scale the Windows node pool to 0:

```powershell
# Stop Windows node pool
az aks nodepool scale `
  --resource-group rg-wincontainer-test `
  --cluster-name aks-wincontainer-test `
  --name winnp `
  --node-count 0

# Resume when needed
az aks nodepool scale `
  --resource-group rg-wincontainer-test `
  --cluster-name aks-wincontainer-test `
  --name winnp `
  --node-count 1
```

### Stop the Entire AKS Cluster (saves maximum cost)

```powershell
# Stop cluster (deallocates all nodes, keeps config)
az aks stop `
  --name aks-wincontainer-test `
  --resource-group rg-wincontainer-test

# Start again when needed
az aks start `
  --name aks-wincontainer-test `
  --resource-group rg-wincontainer-test
```

> Stopping the cluster deallocates all VMs — you only pay for managed OS disks (~$0.04/day total) and ACR storage while stopped.

---

## Part 7 — Update and Redeploy

When you make code changes, just rebuild via ACR and update the deployment:

```powershell
# 1. Build and push new version via ACR (no Docker needed)
az acr build `
  --registry <your-acr-name> `
  --image algorithmapi:v2 `
  --platform windows `
  --file AlgorithmApi/Dockerfile `
  .

# 2. Update the running deployment to the new image
kubectl set image deployment/algorithmapi algorithmapi=<your-acr-name>.azurecr.io/algorithmapi:v2

# 3. Watch rollout
kubectl rollout status deployment/algorithmapi
```

---

## Troubleshooting

### Pod in `ImagePullBackOff`
ACR attachment may have failed. Re-attach:
```powershell
az aks update `
  --name aks-wincontainer-test `
  --resource-group rg-wincontainer-test `
  --attach-acr <your-acr-name>
```

### Pod in `Pending` — no nodes available
Windows node pool may be scaled to 0. Scale it back up:
```powershell
az aks nodepool scale `
  --resource-group rg-wincontainer-test `
  --cluster-name aks-wincontainer-test `
  --name winnp `
  --node-count 1
```

### OS version mismatch error
The Windows node pool OS must match the container base image:
```powershell
# Check node OS version
kubectl get node -o jsonpath='{.items[*].status.nodeInfo.osImage}'

# ltsc2019 nodes  → use mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
# ltsc2022 nodes  → use mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022
```

### View application logs
```powershell
kubectl logs deployment/algorithmapi

# Shell into the running container
kubectl exec -it <pod-name> -- powershell
```

---

## Full Cleanup (Stop All Charges)

```powershell
# Delete the entire resource group — removes AKS, ACR, Load Balancer, disks
az group delete `
  --name rg-wincontainer-test `
  --yes `
  --no-wait
```

---

## Cost Summary

| Resource | SKU | Est. Cost (running) | Est. Cost (stopped) |
|----------|-----|---------------------|---------------------|
| AKS Linux node (Standard_B2s) | 1 node | ~$0.042/hr | ~$0.002/hr (disk) |
| AKS Windows node (Standard_D2s_v3) | 1 node | ~$0.096/hr | ~$0.004/hr (disk) |
| Azure Load Balancer | Basic | ~$0.025/hr | $0 (delete svc) |
| ACR Basic | Basic | ~$0.167/day | ~$0.167/day |
| **Total (running)** | | **~$0.163/hr** | |
| **Total (stopped cluster)** | | | **~$0.17/day** |

> Delete the resource group entirely when the experiment is complete to incur zero ongoing charges.
