# KEDA Troubleshooting CLI Commands

## Table of Contents
1. [KEDA Installation & Verification](#keda-installation--verification)
2. [Prometheus Setup & Verification](#prometheus-setup--verification)
3. [ScaledObject Management](#scaledobject-management)
4. [Metrics Verification](#metrics-verification)
5. [Load Testing](#load-testing)
6. [Debugging & Logs](#debugging--logs)
7. [Resource Inspection](#resource-inspection)
8. [Cleanup Commands](#cleanup-commands)

---

## KEDA Installation & Verification

### Install KEDA
```bash
# Add KEDA Helm repository
helm repo add kedacore https://kedacore.github.io/charts

# Update Helm repositories
helm repo update

# Install KEDA in dedicated namespace
helm install keda kedacore/keda --namespace keda --create-namespace
```

### Verify KEDA Installation
```bash
# Check KEDA pods are running
kubectl get pods -n keda

# Expected output: keda-operator and keda-metrics-apiserver pods in Running state

# Watch KEDA pods until ready
kubectl get pods -n keda -w

# Check KEDA operator logs
kubectl logs -n keda -l app=keda-operator --tail=50

# Check KEDA metrics server logs
kubectl logs -n keda -l app=keda-metrics-apiserver --tail=50

# Verify KEDA CRDs installed
kubectl get crd | grep keda

# Check KEDA version
helm list -n keda
```

---

## Prometheus Setup & Verification

### Install Prometheus
```bash
# Add Prometheus Helm repository
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts

# Update Helm repositories
helm repo update

# Install Prometheus in application namespace
helm install prometheus prometheus-community/prometheus -n keda-http-app

# Install with custom values (for scraping your app)
helm install prometheus prometheus-community/prometheus \
  -n keda-http-app \
  -f K8s/prometheus-values.yaml
```

### Upgrade Prometheus with Custom Scrape Config
```bash
# Upgrade existing Prometheus installation with custom values
helm upgrade prometheus prometheus-community/prometheus \
  -n keda-http-app \
  -f K8s/prometheus-values.yaml

# Verify upgrade
helm list -n keda-http-app
```

### Verify Prometheus Installation
```bash
# Check Prometheus pods
kubectl get pods -n keda-http-app | grep prometheus

# Get Prometheus service details
kubectl get svc -n keda-http-app | grep prometheus

# Get Prometheus server service with full details
kubectl get svc prometheus-server -n keda-http-app -o wide

# Describe Prometheus server service
kubectl describe svc prometheus-server -n keda-http-app

# Check Prometheus server logs
kubectl logs -n keda-http-app -l app.kubernetes.io/name=prometheus,app.kubernetes.io/component=server --tail=50
```

### Access Prometheus UI
```bash
# Port forward Prometheus server to localhost
kubectl port-forward -n keda-http-app svc/prometheus-server 9090:80

# Open in browser: http://localhost:9090
# Check Status → Targets to see scrape targets
# Check Status → Service Discovery to see discovered services
```

### Test Prometheus Connectivity from Cluster
```bash
# Run temporary curl pod to test Prometheus from inside cluster
kubectl run test-curl --rm -it --image=curlimages/curl -n keda-http-app -- \
  curl http://prometheus-server:80/api/v1/query?query=up

# Test specific metric query
kubectl run test-curl --rm -it --image=curlimages/curl -n keda-http-app -- \
  curl "http://prometheus-server:80/api/v1/query?query=http_request_total"
```

---

## ScaledObject Management

### Apply ScaledObject
```bash
# Create/Update ScaledObject
kubectl apply -f K8s/scaledobject.yaml -n keda-http-app

# Apply with explicit namespace
kubectl apply -f K8s/scaledobject.yaml --namespace keda-http-app
```

### Check ScaledObject Status
```bash
# List all ScaledObjects in namespace
kubectl get scaledobject -n keda-http-app

# Get detailed ScaledObject information
kubectl describe scaledobject http-app-scaledobject -n keda-http-app

# Get ScaledObject in YAML format (shows full status)
kubectl get scaledobject http-app-scaledobject -n keda-http-app -o yaml

# Watch ScaledObject status changes
kubectl get scaledobject -n keda-http-app -w

# Check ScaledObject conditions
kubectl get scaledobject http-app-scaledobject -n keda-http-app -o jsonpath='{.status.conditions}'
```

### Delete and Recreate ScaledObject
```bash
# Delete ScaledObject (useful when troubleshooting)
kubectl delete scaledobject http-app-scaledobject -n keda-http-app

# Recreate ScaledObject
kubectl apply -f K8s/scaledobject.yaml -n keda-http-app

# Delete and recreate in one command
kubectl delete scaledobject http-app-scaledobject -n keda-http-app && \
kubectl apply -f K8s/scaledobject.yaml -n keda-http-app
```

---

## Metrics Verification

### Query Prometheus Metrics (Windows)
```powershell
# Query using PowerShell (Windows)
Invoke-WebRequest -Uri "http://localhost:9090/api/v1/query?query=sum(http_request_total)" | Select-Object -ExpandProperty Content

# Query with rate function
Invoke-WebRequest -Uri "http://localhost:9090/api/v1/query?query=sum(rate(http_request_total[1m]))" | Select-Object -ExpandProperty Content

# Query with irate function
Invoke-WebRequest -Uri "http://localhost:9090/api/v1/query?query=sum(irate(http_request_total[2m]))" | Select-Object -ExpandProperty Content
```

### Query Prometheus Metrics (Browser)
```
# Open in browser (easier for Windows)
http://localhost:9090/api/v1/query?query=sum(http_request_total)
http://localhost:9090/api/v1/query?query=sum(rate(http_request_total[1m]))
http://localhost:9090/api/v1/query?query=up
```

### Check Application Metrics Endpoint
```bash
# Port forward application service
kubectl port-forward -n keda-http-app svc/http-app-service 8090:80

# Check metrics endpoint (Windows)
curl http://localhost:8090/metrics

# Check metrics endpoint (PowerShell)
Invoke-WebRequest -Uri "http://localhost:8090/metrics"
```

### Verify Metric Names in Prometheus
```
# In Prometheus UI (http://localhost:9090)
# Type in query box: http_
# Check autocomplete suggestions for available metrics

# Common metric patterns to check:
- http_request_total
- http_requests_total
- http_request_duration_seconds
- fastapi_requests_total
```

---

## Load Testing

### Port Forward Application
```bash
# Forward application service to localhost
kubectl port-forward -n keda-http-app svc/http-app-service 8090:80

# Keep this running in a separate terminal
```

### Using hey (Recommended)
```bash
# Light load (5 concurrent users for 30 seconds)
hey -z 30s -c 5 http://localhost:8090/

# Medium load (50 concurrent users for 60 seconds)
hey -z 60s -c 50 http://localhost:8090/

# Heavy load (100 concurrent users for 120 seconds)
hey -z 120s -c 100 http://localhost:8090/

# Specific number of requests
hey -n 10000 -c 50 http://localhost:8090/

# With custom headers
hey -z 60s -c 50 -H "Content-Type: application/json" http://localhost:8090/
```

### Using Apache Bench
```bash
# 10000 requests with 50 concurrent connections
ab -n 10000 -c 50 http://localhost:8090/

# 1000 requests with 100 concurrent connections
ab -n 1000 -c 100 http://localhost:8090/
```

### Using curl Loop (Windows)
```cmd
# Simple loop (CMD)
for /L %i in (1,1,100) do @curl http://localhost:8090/ > nul 2>&1

# Parallel requests (CMD)
for /L %i in (1,1,100) do @start /B curl http://localhost:8090/ > nul 2>&1
```

### Using PowerShell
```powershell
# Simple loop
1..100 | ForEach-Object { Invoke-WebRequest -Uri "http://localhost:8090/" }

# Parallel requests
1..100 | ForEach-Object { Start-Job -ScriptBlock { Invoke-WebRequest -Uri "http://localhost:8090/" } }
```

---

## Debugging & Logs

### KEDA Operator Logs
```bash
# View KEDA operator logs (last 50 lines)
kubectl logs -n keda -l app=keda-operator --tail=50

# Follow KEDA operator logs in real-time
kubectl logs -n keda -l app=keda-operator -f

# Search for errors in KEDA logs
kubectl logs -n keda -l app=keda-operator --tail=100 | grep -i error

# Search for specific ScaledObject in logs (Windows)
kubectl logs -n keda -l app=keda-operator --tail=100 | findstr /i "http-app"

# Get logs from all KEDA operator pods
kubectl logs -n keda -l app=keda-operator --all-containers=true --tail=50
```

### Application Logs
```bash
# List pods in namespace
kubectl get pods -n keda-http-app

# View application pod logs
kubectl logs <pod-name> -n keda-http-app

# Follow application logs
kubectl logs <pod-name> -n keda-http-app -f

# View logs from all app pods
kubectl logs -n keda-http-app -l app=http-app --tail=50

# View previous pod logs (if pod crashed)
kubectl logs <pod-name> -n keda-http-app --previous
```

### Prometheus Logs
```bash
# View Prometheus server logs
kubectl logs -n keda-http-app -l app.kubernetes.io/name=prometheus,app.kubernetes.io/component=server --tail=50

# Follow Prometheus logs
kubectl logs -n keda-http-app -l app.kubernetes.io/name=prometheus,app.kubernetes.io/component=server -f

# Check for scrape errors
kubectl logs -n keda-http-app -l app.kubernetes.io/name=prometheus,app.kubernetes.io/component=server --tail=100 | grep -i error
```

### HPA Logs and Events
```bash
# Describe HPA (shows scaling events)
kubectl describe hpa keda-hpa-http-app-scaledobject -n keda-http-app

# Get HPA events
kubectl get events -n keda-http-app --field-selector involvedObject.name=keda-hpa-http-app-scaledobject

# Watch HPA events in real-time
kubectl get events -n keda-http-app -w
```

---

## Resource Inspection

### List All Resources in Namespace
```bash
# List all resources
kubectl get all -n keda-http-app

# List with more details
kubectl get all -n keda-http-app -o wide

# List specific resource types
kubectl get deployments,services,pods,hpa,scaledobject -n keda-http-app
```

### Check Deployments
```bash
# List deployments
kubectl get deployment -n keda-http-app

# Describe deployment
kubectl describe deployment http-app -n keda-http-app

# Get deployment in YAML
kubectl get deployment http-app -n keda-http-app -o yaml

# Check deployment rollout status
kubectl rollout status deployment http-app -n keda-http-app
```

### Check Services
```bash
# List services
kubectl get svc -n keda-http-app

# Get service details
kubectl get svc http-app-service -n keda-http-app -o wide

# Describe service
kubectl describe svc http-app-service -n keda-http-app

# Check service endpoints
kubectl get endpoints http-app-service -n keda-http-app
```

### Check Pods
```bash
# List pods
kubectl get pods -n keda-http-app

# List pods with labels
kubectl get pods -n keda-http-app --show-labels

# Watch pods (see scaling in real-time)
kubectl get pods -n keda-http-app -w

# Get pod details
kubectl describe pod <pod-name> -n keda-http-app

# Check pod resource usage
kubectl top pods -n keda-http-app
```

### Check HPA (Horizontal Pod Autoscaler)
```bash
# List HPA
kubectl get hpa -n keda-http-app

# Watch HPA changes
kubectl get hpa -n keda-http-app -w

# Describe HPA (shows current/target metrics)
kubectl describe hpa keda-hpa-http-app-scaledobject -n keda-http-app

# Get HPA in YAML format
kubectl get hpa keda-hpa-http-app-scaledobject -n keda-http-app -o yaml

# Check HPA metrics
kubectl get hpa keda-hpa-http-app-scaledobject -n keda-http-app -o jsonpath='{.status.currentMetrics}'
```

### Check ConfigMaps
```bash
# List ConfigMaps
kubectl get configmap -n keda-http-app

# View Prometheus ConfigMap
kubectl get configmap prometheus-server -n keda-http-app -o yaml

# Check scrape configs in Prometheus
kubectl get configmap prometheus-server -n keda-http-app -o yaml | grep -A 10 "scrape_configs"
```

### Check Service Discovery
```bash
# Check if Prometheus can discover services
kubectl get configmap prometheus-server -n keda-http-app -o yaml | grep -A 10 "kubernetes_sd_configs"

# List all endpoints (what Prometheus can scrape)
kubectl get endpoints -n keda-http-app
```

---

## Monitoring Commands

### Watch Multiple Resources Simultaneously
```bash
# Terminal 1: Watch pods
kubectl get pods -n keda-http-app -w

# Terminal 2: Watch HPA
kubectl get hpa -n keda-http-app -w

# Terminal 3: Watch ScaledObject
kubectl get scaledobject -n keda-http-app -w

# Terminal 4: Watch events
kubectl get events -n keda-http-app -w
```

### Check Resource Utilization
```bash
# Check node resources
kubectl top nodes

# Check pod CPU/Memory usage
kubectl top pods -n keda-http-app

# Check pod resource limits
kubectl describe pod <pod-name> -n keda-http-app | grep -A 5 "Limits"
```

### Check Scaling History
```bash
# Get HPA events (shows scaling decisions)
kubectl describe hpa keda-hpa-http-app-scaledobject -n keda-http-app | grep -A 20 "Events"

# Get all events in namespace
kubectl get events -n keda-http-app --sort-by='.lastTimestamp'

# Filter scaling events
kubectl get events -n keda-http-app --field-selector reason=ScalingReplicaSet
```

---

## Cleanup Commands

### Delete ScaledObject
```bash
# Delete ScaledObject (HPA will be auto-deleted)
kubectl delete scaledobject http-app-scaledobject -n keda-http-app

# Delete using file
kubectl delete -f K8s/scaledobject.yaml -n keda-http-app
```

### Delete Application Resources
```bash
# Delete deployment
kubectl delete deployment http-app -n keda-http-app

# Delete service
kubectl delete service http-app-service -n keda-http-app

# Delete all app resources
kubectl delete -f K8s/deployment.yaml -n keda-http-app
kubectl delete -f K8s/services.yaml -n keda-http-app
```

### Uninstall Prometheus
```bash
# Uninstall Prometheus Helm release
helm uninstall prometheus -n keda-http-app

# Delete Prometheus PVCs (if any)
kubectl delete pvc -n keda-http-app -l app.kubernetes.io/name=prometheus
```

### Uninstall KEDA
```bash
# Uninstall KEDA Helm release
helm uninstall keda -n keda

# Delete KEDA namespace
kubectl delete namespace keda
```

### Delete Namespace
```bash
# Delete entire namespace (removes all resources)
kubectl delete namespace keda-http-app

# Warning: This deletes everything in the namespace!
```

### Complete Cleanup
```bash
# Delete ScaledObject
kubectl delete scaledobject http-app-scaledobject -n keda-http-app

# Uninstall Prometheus
helm uninstall prometheus -n keda-http-app

# Delete application resources
kubectl delete deployment http-app -n keda-http-app
kubectl delete service http-app-service -n keda-http-app

# Delete namespace
kubectl delete namespace keda-http-app

# Uninstall KEDA
helm uninstall keda -n keda
kubectl delete namespace keda
```

---

## Quick Diagnostic Commands

### One-Liner Health Checks
```bash
# Check if KEDA is running
kubectl get pods -n keda | grep -i running

# Check if ScaledObject is ready
kubectl get scaledobject -n keda-http-app -o jsonpath='{.items[0].status.conditions[?(@.type=="Ready")].status}'

# Check current replica count
kubectl get deployment http-app -n keda-http-app -o jsonpath='{.spec.replicas}'

# Check HPA target metrics
kubectl get hpa -n keda-http-app -o jsonpath='{.items[0].status.currentMetrics}'

# Check Prometheus targets count
kubectl exec -n keda-http-app -it $(kubectl get pod -n keda-http-app -l app.kubernetes.io/name=prometheus -o jsonpath='{.items[0].metadata.name}') -- wget -qO- http://localhost:9090/api/v1/targets | grep -c "\"health\":\"up\""
```

### Troubleshooting Checklist Commands
```bash
# 1. Is KEDA installed?
kubectl get pods -n keda

# 2. Is deployment running?
kubectl get deployment http-app -n keda-http-app

# 3. Is service created?
kubectl get svc http-app-service -n keda-http-app

# 4. Is Prometheus running?
kubectl get pods -n keda-http-app | grep prometheus-server

# 5. Is ScaledObject ready?
kubectl get scaledobject -n keda-http-app

# 6. Is HPA created?
kubectl get hpa -n keda-http-app

# 7. Are metrics available?
# Open: http://localhost:9090 (after port-forward)

# 8. Any errors in KEDA logs?
kubectl logs -n keda -l app=keda-operator --tail=20 | grep -i error
```

---

## Advanced Debugging

### Execute Commands in Pods
```bash
# Execute shell in application pod
kubectl exec -it <pod-name> -n keda-http-app -- /bin/sh

# Execute shell in Prometheus pod
kubectl exec -it <prometheus-pod-name> -n keda-http-app -- /bin/sh

# Run curl from inside pod
kubectl exec -it <pod-name> -n keda-http-app -- curl http://prometheus-server/api/v1/query?query=up
```

### Network Debugging
```bash
# Test DNS resolution
kubectl run test-dns --rm -it --image=busybox -n keda-http-app -- nslookup prometheus-server

# Test connectivity to Prometheus
kubectl run test-curl --rm -it --image=curlimages/curl -n keda-http-app -- curl -v http://prometheus-server:80

# Test connectivity to application
kubectl run test-curl --rm -it --image=curlimages/curl -n keda-http-app -- curl -v http://http-app-service:80/metrics
```

### YAML Validation
```bash
# Validate YAML syntax
kubectl apply -f K8s/scaledobject.yaml --dry-run=client

# Validate and show what would be created
kubectl apply -f K8s/scaledobject.yaml --dry-run=server -o yaml
```

---

## Useful Aliases (Optional)

```bash
# Add to ~/.bashrc or ~/.zshrc

# Kubectl shortcuts
alias k='kubectl'
alias kgp='kubectl get pods'
alias kgs='kubectl get svc'
alias kgd='kubectl get deployment'
alias kgh='kubectl get hpa'
alias kgso='kubectl get scaledobject'

# Namespace shortcuts
alias kn='kubectl config set-context --current --namespace'
alias kns='kubectl get namespaces'

# KEDA specific
alias kedapods='kubectl get pods -n keda'
alias kedalogs='kubectl logs -n keda -l app=keda-operator --tail=50'
alias kedaso='kubectl get scaledobject -n keda-http-app'

# Watch shortcuts
alias watchpods='kubectl get pods -n keda-http-app -w'
alias watchhpa='kubectl get hpa -n keda-http-app -w'
```

---

## Tips & Best Practices

1. **Always check KEDA logs first** when troubleshooting scaling issues
2. **Use `-w` flag** to watch resources in real-time during load tests
3. **Port-forward in separate terminals** to keep connections stable
4. **Verify metrics in Prometheus UI** before configuring ScaledObject
5. **Use `describe` commands** to see events and detailed status
6. **Check HPA status** to see current vs target metrics
7. **Use proper load testing tools** (hey, ab) for consistent results
8. **Wait for cooldown period** before expecting scale-down
9. **Check all three**: ScaledObject, HPA, and Deployment status
10. **Use `--tail` flag** to limit log output for faster debugging
