# Kubernetes Ingress Demo (Azure AKS)

Same path-based routing demo as the Docker Desktop setup, deployed on Azure AKS.

## Architecture

```
<load-balancer-ip>/orders/*     → orders-service     (1 replica)
<load-balancer-ip>/inventory/*  → inventory-service  (1 replica)
<load-balancer-ip>/customers/*  → customers-service  (3 replicas)
```

## Prerequisites

- Azure CLI installed and logged in
- `kubectl` installed

Verify:
```bash
az --version
az login
kubectl version --client
```

## 1. Create Resource Group & AKS Cluster

```bash
az group create --name ingress-demo-rg --location eastus

az aks create \
  --resource-group ingress-demo-rg \
  --name ingress-demo \
  --node-count 2 \
  --node-vm-size Standard_B2s \
  --generate-ssh-keys
```

This takes ~5 minutes. Then update your kubeconfig:

```bash
az aks get-credentials --resource-group ingress-demo-rg --name ingress-demo
```

Verify the cluster is active:
```bash
kubectl get nodes
```

## 2. Install NGINX Ingress Controller

On AKS, the same manifest provisions an Azure Load Balancer automatically:

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.1/deploy/static/provider/cloud/deploy.yaml
```

Wait until the controller pod is ready:
```bash
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=120s
```

Get the Load Balancer public IP (you'll need this to test):
```bash
kubectl get svc -n ingress-nginx ingress-nginx-controller
```

Note the `EXTERNAL-IP` value — this is your `<load-balancer-ip>`.

> IP assignment may take 2–3 minutes after the Load Balancer is provisioned.

## 3. Deploy

```bash
kubectl apply -f .
```

Verify everything is running:
```bash
kubectl get deployments
kubectl get services
kubectl get ingress
```

## 4. Test

Replace `<load-balancer-ip>` with the `EXTERNAL-IP` from step 2:

```bash
curl http://<load-balancer-ip>/orders
curl http://<load-balancer-ip>/inventory
curl http://<load-balancer-ip>/customers
```

Expected responses:
| Endpoint | Response |
|---|---|
| `/orders` | 📦 Orders API - Connection Successful |
| `/inventory` | 📦 Inventory API - Connection Successful |
| `/customers` | 👤 Customer API - Connection Successful |

## 5. Teardown

Remove the app manifests:
```bash
kubectl delete -f .
```

Remove the NGINX Ingress Controller:
```bash
kubectl delete -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.1/deploy/static/provider/cloud/deploy.yaml
```

Delete the AKS cluster and resource group (removes all resources including the Load Balancer):
```bash
az group delete --name ingress-demo-rg --yes --no-wait
```

> Always delete the resource group when done to avoid ongoing VM and Load Balancer charges.

## Notes

- Unlike Docker Desktop, AKS provisions a real Azure Load Balancer — this incurs cost.
- The same manifests work on AKS without any changes since `ingress-router.yaml` uses `ingressClassName: nginx`.
- `Standard_B2s` nodes are sufficient for this demo and are cost-effective for learning.
- AKS gives you a public IP (not a DNS name like EKS) as the `EXTERNAL-IP`.
