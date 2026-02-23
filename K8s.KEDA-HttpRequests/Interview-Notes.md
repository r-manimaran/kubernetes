# KEDA Interview Quick Reference Guide

## 🎯 What is KEDA?

**KEDA** = Kubernetes Event-Driven Autoscaling
- Open-source CNCF project
- Extends Kubernetes HPA (Horizontal Pod Autoscaler)
- Scales workloads based on external metrics/events
- Supports 50+ scalers (Prometheus, Kafka, RabbitMQ, Azure Queue, AWS SQS, etc.)

---

## 🔑 Key Concepts

### 1. ScaledObject
- Custom Resource Definition (CRD) for scaling Deployments
- Defines scaling rules and triggers
- Automatically creates HPA behind the scenes
- Can scale to zero (0 pods when idle)

### 2. ScaledJob
- For scaling Kubernetes Jobs (batch processing)
- Creates jobs based on queue length or events
- Useful for background tasks, data processing

### 3. Triggers
- Define what metric to monitor
- Examples: Prometheus, CPU, Memory, Cron, HTTP, Kafka lag
- Multiple triggers = composite scaling (AND/OR logic)

### 4. Scalers
- Built-in integrations for external systems
- 50+ scalers available
- Custom external scalers possible

---

## 📊 KEDA vs Native HPA

| Feature | Native HPA | KEDA |
|---------|-----------|------|
| Metrics | CPU/Memory only | 50+ external sources |
| Scale to Zero | ❌ No | ✅ Yes |
| Event-Driven | ❌ No | ✅ Yes |
| Setup Complexity | Simple | Moderate |
| Use Case | Basic scaling | Advanced, event-driven |

---

## 🏗️ Architecture Components

### 1. KEDA Operator
- Watches ScaledObjects/ScaledJobs
- Creates and manages HPAs
- Activates/deactivates deployments

### 2. Metrics Server
- Exposes external metrics to Kubernetes
- Acts as Kubernetes metrics API server
- HPA queries this for scaling decisions

### 3. Admission Webhooks
- Validates ScaledObject configurations
- Mutates resources if needed

---

## 📝 ScaledObject Structure

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: app-scaledobject
  namespace: default
spec:
  scaleTargetRef:           # What to scale
    name: my-deployment
    kind: Deployment
  minReplicaCount: 1        # Minimum pods (0 for scale-to-zero)
  maxReplicaCount: 10       # Maximum pods
  pollingInterval: 30       # How often to check metrics (seconds)
  cooldownPeriod: 300       # Wait before scaling down (seconds)
  triggers:                 # Scaling triggers
  - type: prometheus
    metadata:
      serverAddress: http://prometheus:9090
      query: sum(rate(http_requests_total[2m]))
      threshold: '100'
```

---

## 🎛️ Important Parameters

### pollingInterval
- How often KEDA checks metrics
- Default: 30 seconds
- Lower = faster reaction, higher load on metrics source

### cooldownPeriod
- Wait time before scaling down
- Default: 300 seconds (5 minutes)
- Prevents flapping (rapid scale up/down)

### minReplicaCount
- Minimum pods to maintain
- Set to 0 for scale-to-zero
- Default: 0

### maxReplicaCount
- Maximum pods allowed
- Cost control mechanism
- Required field

---

## 🔥 Common Triggers (Interview Favorites)

### 1. Prometheus
```yaml
triggers:
- type: prometheus
  metadata:
    serverAddress: http://prometheus:9090
    query: sum(rate(http_requests_total[2m]))
    threshold: '100'
```

### 2. CPU/Memory
```yaml
triggers:
- type: cpu
  metricType: Utilization
  metadata:
    value: "70"
```

### 3. Kafka
```yaml
triggers:
- type: kafka
  metadata:
    bootstrapServers: kafka:9092
    consumerGroup: my-group
    topic: orders
    lagThreshold: '10'
```

### 4. Cron (Time-based)
```yaml
triggers:
- type: cron
  metadata:
    timezone: UTC
    start: 0 8 * * *      # 8 AM
    end: 0 18 * * *       # 6 PM
    desiredReplicas: "10"
```

### 5. RabbitMQ
```yaml
triggers:
- type: rabbitmq
  metadata:
    host: amqp://rabbitmq:5672
    queueName: tasks
    queueLength: '20'
```

---

## 🚀 Scale-to-Zero Feature

**Key Differentiator from HPA**

### How it Works:
1. When no events/metrics → scales to 0 pods
2. KEDA monitors the trigger source
3. When event arrives → activates deployment (scales to minReplicas)
4. Deployment handles requests
5. When idle again → scales back to 0

### Use Cases:
- Development environments
- Batch processing jobs
- Infrequent workloads
- Cost optimization

### Configuration:
```yaml
spec:
  minReplicaCount: 0        # Enable scale-to-zero
  idleReplicaCount: 0       # Replicas when idle
```

---

## 🔐 Authentication (TriggerAuthentication)

For secure access to external systems:

```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: prometheus-auth
spec:
  secretTargetRef:
  - parameter: username
    name: prometheus-secret
    key: username
  - parameter: password
    name: prometheus-secret
    key: password
```

Reference in ScaledObject:
```yaml
triggers:
- type: prometheus
  authenticationRef:
    name: prometheus-auth
```

---

## 📈 Scaling Behavior

### Scale Up:
- Immediate when threshold exceeded
- Follows HPA scale-up policies
- Can be aggressive

### Scale Down:
- Waits for cooldownPeriod
- Gradual (default: 1 pod per minute)
- Prevents flapping

### Formula:
```
desiredReplicas = ceil(currentReplicas × (currentMetric / targetMetric))
```

---

## 🎯 Real-World Use Cases

### 1. HTTP Traffic Scaling
- Scale web apps based on request rate
- Use Prometheus or HTTP Add-on
- Threshold: requests per second

### 2. Queue Processing
- Scale workers based on queue depth
- Kafka, RabbitMQ, SQS, Azure Queue
- Threshold: messages in queue

### 3. Database Load
- Scale based on connection pool usage
- Custom metrics from database
- Threshold: active connections

### 4. Scheduled Scaling
- Scale up during business hours
- Scale down at night/weekends
- Use Cron trigger

### 5. Multi-Metric Scaling
- Combine CPU + Queue depth
- Prometheus + Memory
- Complex business logic

---

## 🛠️ Installation & Setup

### Install KEDA:
```bash
helm repo add kedacore https://kedacore.github.io/charts
helm install keda kedacore/keda --namespace keda --create-namespace
```

### Verify Installation:
```bash
kubectl get pods -n keda
# Should see: keda-operator, keda-metrics-apiserver
```

### Apply ScaledObject:
```bash
kubectl apply -f scaledobject.yaml
```

### Check Status:
```bash
kubectl get scaledobject
kubectl get hpa
```

---

## 🐛 Troubleshooting Quick Tips

### ScaledObject Not Ready
- Check KEDA operator logs: `kubectl logs -n keda -l app=keda-operator`
- Verify deployment exists
- Check trigger authentication

### Pods Not Scaling
- Verify metric query returns data
- Check threshold values
- Ensure HPA is created
- Review cooldownPeriod

### Flapping (Rapid Scale Up/Down)
- Increase cooldownPeriod
- Increase pollingInterval
- Adjust threshold values
- Add stabilizationWindowSeconds

### Metrics Not Available
- Check external system connectivity
- Verify authentication
- Test metric query manually

---

## 💡 Interview Questions & Answers

### Q1: What is KEDA and why use it?
**A:** KEDA is Kubernetes Event-Driven Autoscaling that extends HPA to support 50+ external metrics sources. Use it for event-driven workloads, scale-to-zero capability, and complex scaling scenarios beyond CPU/Memory.

### Q2: How does KEDA differ from HPA?
**A:** KEDA extends HPA by supporting external metrics (Kafka, Prometheus, queues), scale-to-zero, and event-driven scaling. HPA only supports CPU/Memory metrics and can't scale to zero.

### Q3: Explain scale-to-zero
**A:** Scale-to-zero reduces pods to 0 when idle, saving costs. KEDA monitors the event source and activates the deployment when events arrive. Useful for batch jobs and infrequent workloads.

### Q4: What are triggers and scalers?
**A:** Triggers define what metric to monitor (e.g., Prometheus query). Scalers are built-in integrations for external systems (50+ available like Kafka, RabbitMQ, AWS SQS).

### Q5: How to prevent flapping?
**A:** Use cooldownPeriod (wait before scale-down), increase pollingInterval, adjust thresholds, and configure stabilizationWindowSeconds in HPA behavior.

### Q6: Can you use multiple triggers?
**A:** Yes, multiple triggers enable composite scaling. By default, it's OR logic (any trigger can scale). All triggers must be active to scale down.

### Q7: How does KEDA handle authentication?
**A:** Use TriggerAuthentication CRD to store credentials in Kubernetes Secrets. Reference it in ScaledObject to securely access external systems.

### Q8: What happens if metrics source is unavailable?
**A:** Configure fallback behavior with failureThreshold and fallback replicas. KEDA can maintain a safe replica count during outages.

### Q9: KEDA vs KEDA HTTP Add-on?
**A:** KEDA HTTP Add-on is specifically for HTTP workloads, provides request queuing, and better scale-to-zero for HTTP apps. Regular KEDA is general-purpose.

### Q10: How to monitor KEDA itself?
**A:** KEDA exposes Prometheus metrics at `:8080/metrics`. Monitor `keda_scaler_errors`, `keda_scaled_object_errors`, and `keda_scaler_metrics_value`.

---

## 🎓 Key Takeaways for Interviews

1. **KEDA = Event-Driven Autoscaling** for Kubernetes
2. **Extends HPA** with 50+ external metrics sources
3. **Scale-to-zero** is the killer feature
4. **ScaledObject** creates HPA automatically
5. **Triggers** define what to monitor
6. **Scalers** are pre-built integrations
7. **TriggerAuthentication** for secure access
8. **cooldownPeriod** prevents flapping
9. **pollingInterval** controls check frequency
10. **Production-ready** CNCF graduated project

---

## 📚 Common Prometheus Queries (Interview Favorite)

### Request Rate:
```promql
sum(rate(http_requests_total[2m]))
```

### Error Rate:
```promql
sum(rate(http_requests_total{status=~"5.."}[2m]))
```

### Latency (95th percentile):
```promql
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[2m])) by (le))
```

### Queue Depth:
```promql
sum(queue_depth)
```

---

## 🔗 Important Links

- **Official Docs:** https://keda.sh
- **GitHub:** https://github.com/kedacore/keda
- **Scalers List:** https://keda.sh/docs/scalers/
- **CNCF Project:** Graduated (Production-ready)

---

## ⚡ Quick Commands Cheat Sheet

```bash
# Install KEDA
helm install keda kedacore/keda --namespace keda --create-namespace

# Check KEDA
kubectl get pods -n keda

# Apply ScaledObject
kubectl apply -f scaledobject.yaml

# Check Status
kubectl get scaledobject
kubectl get hpa
kubectl describe scaledobject <name>

# View Logs
kubectl logs -n keda -l app=keda-operator

# Delete ScaledObject
kubectl delete scaledobject <name>

# Uninstall KEDA
helm uninstall keda -n keda
```

---

## 🎯 Pro Tips

1. Always test metric queries in Prometheus UI first
2. Start with conservative thresholds, tune based on observation
3. Use cooldownPeriod to prevent cost spikes
4. Monitor KEDA metrics in production
5. Use TriggerAuthentication for secrets
6. Document scaling decisions and thresholds
7. Test scale-to-zero behavior thoroughly
8. Set maxReplicaCount for cost control
9. Use multiple triggers for complex scenarios
10. Keep KEDA updated (active development)

---

**Good luck with your interview! 🚀**
