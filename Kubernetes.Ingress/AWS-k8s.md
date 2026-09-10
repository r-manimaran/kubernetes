# Kubernetes Ingress Demo (AWS EKS)

Same path-based routing demo as the Docker Desktop setup, deployed on AWS EKS.

## Architecture

```
<load-balancer-dns>/orders/*     → orders-service     (1 replica)
<load-balancer-dns>/inventory/*  → inventory-service  (1 replica)
<load-balancer-dns>/customers/*  → customers-service  (3 replicas)
```

## Prerequisites

- AWS CLI installed and configured (`aws configure`)
- `eksctl` installed — [install guide](https://eksctl.io/installation/)
- `kubectl` installed

Verify:
```bash
aws sts get-caller-identity
eksctl version
kubectl version --client
```

## 1. Create EKS Cluster

```bash
eksctl create cluster \
  --name ingress-demo \
  --region us-east-1 \
  --nodegroup-name demo-nodes \
  --node-type t3.small \
  --nodes 2
```

This takes ~15 minutes. It also automatically updates your kubeconfig.

Verify the cluster is active:
```bash
kubectl get nodes
```

## 2. Install NGINX Ingress Controller

On EKS, the same manifest provisions an AWS Network Load Balancer automatically:

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

Get the Load Balancer DNS (you'll need this to test):
```bash
kubectl get svc -n ingress-nginx ingress-nginx-controller
```

Note the `EXTERNAL-IP` value — this is your `<load-balancer-dns>`.

> DNS propagation may take 2–3 minutes after the Load Balancer is provisioned.

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

Replace `<load-balancer-dns>` with the `EXTERNAL-IP` from step 2:

```bash
curl http://<load-balancer-dns>/orders
curl http://<load-balancer-dns>/inventory
curl http://<load-balancer-dns>/customers
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

Delete the EKS cluster (also removes the Load Balancer):
```bash
eksctl delete cluster --name ingress-demo --region us-east-1
```

> Always delete the cluster when done to avoid ongoing EC2 and Load Balancer charges.

## Notes

- Unlike Docker Desktop, EKS provisions a real AWS Network Load Balancer — this incurs cost.
- The same manifests work on EKS without any changes since `ingress-router.yaml` uses `ingressClassName: nginx`.
- `t3.small` nodes are sufficient for this demo. Use `t3.micro` only if staying within Free Tier limits (not recommended for EKS worker nodes).
