# Jenkins with Kubernetes Pod Agents for .NET Docker Builds

Build a .NET Web API Docker image with Jenkins, using short-lived Kubernetes pods as build agents instead of a long-running VM. The same pattern is rehearsed locally (Docker Desktop Kubernetes + Jenkins in Docker) and then moved to AKS with a corporate-managed Jenkins.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Design Decision: Building Images Inside a Pod](#design-decision-building-images-inside-a-pod)
5. [Repository Layout](#repository-layout)
6. [Kubernetes Manifests](#kubernetes-manifests)
7. [Local Setup (Docker Desktop)](#local-setup-docker-desktop)
8. [Network Verification](#network-verification)
9. [Jenkins Configuration](#jenkins-configuration)
10. [Smoke Test Pipeline](#smoke-test-pipeline)
11. [Pipeline: Push to Azure Container Registry (ACR)](#pipeline-push-to-azure-container-registry-acr)
12. [Pipeline: Push to AWS ECR](#pipeline-push-to-aws-ecr)
13. [AKS Setup Checklist (Corporate Jenkins)](#aks-setup-checklist-corporate-jenkins)
14. [Information to Share with the Jenkins Team](#information-to-share-with-the-jenkins-team)
15. [Troubleshooting](#troubleshooting)
16. [Cleanup](#cleanup)

---

## Overview

Goal: replace a VM-based Jenkins build agent with Kubernetes pods that are created for each build and deleted afterwards.

- **Jenkins controller** creates an agent pod per build through the Kubernetes plugin.
- **The pod** has several containers sharing one workspace: `jnlp` (Jenkins agent, added automatically), `dotnet` (restore/build/test), and `kaniko` (image build and push).
- **The registry** receives the image: Azure Container Registry (ACR) or AWS ECR.

Two environments are covered:

| Environment | Jenkins | Kubernetes |
|---|---|---|
| Local rehearsal | Docker container, `http://localhost:8080` | Docker Desktop Kubernetes |
| Corporate target | Managed by the corporate Jenkins team | AKS cluster managed by you |

## Architecture

```
GitHub ──webhook/poll──▶ Jenkins controller
                              │ creates pod via Kubernetes plugin
                              ▼
                 ┌──────── Agent pod (namespace: jenkins-agents) ────────┐
                 │  jnlp container   (connects back to Jenkins)          │
                 │  dotnet container (restore / build / test)            │
                 │  kaniko container (build image, push to registry)     │
                 │  shared emptyDir workspace                            │
                 └───────────────────────────────┬───────────────────────┘
                                                 ▼
                                     ACR  or  AWS ECR
```

Connections that must work:

1. Jenkins controller → Kubernetes API server (create/delete pods, exec, logs).
2. Agent pod → Jenkins URL (WebSocket or JNLP) to register as an agent.
3. Agent pod → GitHub, NuGet feeds, and the container registry (outbound).

## Prerequisites

- Jenkins (LTS) with the **Kubernetes** plugin installed.
- A Kubernetes cluster (Docker Desktop locally, AKS for the target environment) and `kubectl` access.
- A .NET Web API in GitHub with a `Dockerfile`.
- A container registry (ACR or ECR) and an identity allowed to push.
- Docker Desktop resources (local): at least 4 CPUs and 6-8 GB memory, since .NET builds in Kaniko are heavy.

## Design Decision: Building Images Inside a Pod

On a VM agent, Jenkins used the host Docker daemon. AKS nodes use containerd and expose no Docker socket, and mounting a socket or running privileged Docker-in-Docker is a security risk. Use a daemonless builder:

| Option | Notes |
|---|---|
| **Kaniko** (used here) | No daemon, no privileged mode. The original Google project is archived; a maintained fork exists under Chainguard. |
| BuildKit (rootless) | Faster with better caching, slightly more setup. |
| ACR Tasks (`az acr build`) | Offloads the build to Azure; the pod only triggers it. |

Consequence: shared-library steps that call the `docker` CLI (for example `dockerBuild` and `ecrPush`) do not work in the pod. They are replaced by a single Kaniko stage that builds and pushes.

## Repository Layout

```
jenkins-agents/
├── 00-namespace.yaml
├── 01-rbac.yaml
├── 02-limits.yaml
├── 03-networkpolicy.optional.yaml
├── acr-secret.template.yaml
└── kustomization.yaml
Jenkinsfile
README.md
```

## Kubernetes Manifests

| File | Purpose |
|---|---|
| `00-namespace.yaml` | Creates the `jenkins-agents` namespace. |
| `01-rbac.yaml` | `jenkins` ServiceAccount, namespace-scoped Role (pods, pods/exec, pods/log, events), RoleBinding, and a token Secret. |
| `02-limits.yaml` | ResourceQuota (max 10 pods plus CPU/memory caps) and LimitRange (defaults for containers that declare none, such as `jnlp`). |
| `03-networkpolicy.optional.yaml` | Optional: denies inbound traffic to agent pods. Needs a CNI that enforces NetworkPolicy. |
| `acr-secret.template.yaml` | Template for the registry secret `acr-cred`. Never commit real credentials. |
| `kustomization.yaml` | Bundles files 00 to 02 for a single `kubectl apply -k .`. |

Apply:

```bash
# Namespace, RBAC, quota and limits
kubectl apply -k .

# Registry secret (fill in the template first, keep it out of source control)
kubectl apply -f acr-secret.template.yaml

# Optional network policy
kubectl apply -f 03-networkpolicy.optional.yaml
```

Equivalent one-line alternative for the registry secret:

```bash
kubectl create secret docker-registry acr-cred -n jenkins-agents \
  --docker-server=<acrName>.azurecr.io \
  --docker-username=<appId-or-token-name> \
  --docker-password=<password>
```

Read the ServiceAccount token (share it only through a secure channel):

```bash
kubectl get secret jenkins-token -n jenkins-agents -o jsonpath='{.data.token}' | base64 -d
```

Verify permissions:

```bash
kubectl auth can-i create pods -n jenkins-agents \
  --as=system:serviceaccount:jenkins-agents:jenkins
```

## Local Setup (Docker Desktop)

1. Enable Kubernetes in Docker Desktop (Settings → Kubernetes).
2. Select the context and apply the manifests:
   ```bash
   kubectl config use-context docker-desktop
   kubectl get nodes
   kubectl apply -k .
   ```
3. Jenkins runs in a Docker container with its port published on all interfaces, for example `-p 8080:8080` (not `-p 127.0.0.1:8080:8080`), and a persistent volume for `/var/jenkins_home`.

Key local addresses:

| Purpose | Address |
|---|---|
| Jenkins UI from your browser | `http://localhost:8080` |
| Jenkins URL as seen from agent pods | `http://host.docker.internal:8080` |
| Kubernetes API as seen from the Jenkins container | `https://kubernetes.docker.internal:6443` |

Use the **host** port (left side of `docker ps` output) when pods call Jenkins. Pods cannot use `localhost`, because inside a pod it refers to the pod itself.

## Network Verification

Test both directions before configuring Jenkins.

**Agent pod to Jenkins:**

```bash
kubectl run curltest -n jenkins-agents --restart=Never \
  --image=curlimages/curl -- curl -sS -m 10 -I http://host.docker.internal:8080/login
kubectl logs curltest -n jenkins-agents
kubectl delete pod curltest -n jenkins-agents
```

Expected: `HTTP/1.1 200 OK` (or `403`). Avoid `-it` here; the pod can exit before the attach completes, hiding the real output.

**Jenkins container to the Kubernetes API:**

```bash
docker exec <jenkins-container-name> curl -sk https://kubernetes.docker.internal:6443/version
```

Expected: a JSON version response (or 401/403).

## Jenkins Configuration

1. Install the **Kubernetes** plugin (Manage Jenkins → Plugins) and restart if prompted.
2. Add the ServiceAccount token as a **Secret text** credential with ID `k8s-token`.
3. Manage Jenkins → Clouds → New cloud → **Kubernetes**:

| Field | Value (local) |
|---|---|
| Kubernetes URL | `https://kubernetes.docker.internal:6443` |
| Disable https certificate check | Tick (local only; use the cluster CA in other environments) |
| Kubernetes Namespace | `jenkins-agents` (exact spelling, with the trailing "s") |
| Credentials | `k8s-token` |
| WebSocket | Tick |
| Jenkins URL | `http://host.docker.internal:8080` |

4. Click **Test Connection**. Expected: "Connected to Kubernetes v1.x.x".

## Smoke Test Pipeline

Run this before the real pipeline to prove the Jenkins → cluster → agent path works.

```groovy
pipeline {
  agent {
    kubernetes {
      namespace 'jenkins-agents'
      yaml '''
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:8.0
    command: ["sleep"]
    args: ["infinity"]
'''
    }
  }
  stages {
    stage('Smoke test') {
      steps { container('dotnet') { sh 'dotnet --info' } }
    }
  }
}
```

Watch the pod being created and removed:

```bash
kubectl get pods -n jenkins-agents -w
```

## Pipeline: Push to Azure Container Registry (ACR)

Uses the shared library steps for greeting, checkout and restore, and Kaniko for build and push. Registry credentials come from the `acr-cred` Kubernetes secret mounted into the Kaniko container, so no registry credentials are stored in Jenkins.

```groovy
@Library('jenkins-shared-library') _

pipeline {
  agent {
    kubernetes {
      namespace 'jenkins-agents'
      defaultContainer 'jnlp'
      yaml '''
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:8.0
    command: ["sleep"]
    args: ["infinity"]
    resources:
      requests: { cpu: "500m", memory: 1Gi }
      limits:   { cpu: "2",    memory: 3Gi }
  - name: kaniko
    image: gcr.io/kaniko-project/executor:debug
    command: ["sleep"]
    args: ["infinity"]
    resources:
      requests: { cpu: "500m", memory: 1Gi }
      limits:   { cpu: "2",    memory: 4Gi }
    volumeMounts:
    - name: docker-config
      mountPath: /kaniko/.docker
  volumes:
  - name: docker-config
    secret:
      secretName: acr-cred
      items:
      - key: .dockerconfigjson
        path: config.json
'''
    }
  }

  environment {
    ACR_NAME       = '<acrName>'
    ACR_REGISTRY   = "${ACR_NAME}.azurecr.io"
    IMAGE_NAME     = 'ordersapi'
    ACR_IMAGE      = "${ACR_REGISTRY}/${IMAGE_NAME}"
    // Folder that contains the Dockerfile (adjust to your repo layout)
    DOCKER_CONTEXT = 'Jenkins-Smart-Utils/Microservices/OrdersApi'
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
  }

  stages {
    stage('Greetings') {
      steps { helloWorld("Message from Pipeline") }
    }

    stage('Checkout') {
      steps { checkoutGit('https://github.com/r-manimaran/Full-Stack-App.git', 'main') }
    }

    stage('Restore') {
      steps {
        container('dotnet') {
          dotnetBuild('Jenkins-Smart-Utils/Microservices/OrdersApi', 'OrdersApi.csproj')
        }
      }
    }

    stage('Docker Build & ACR Push') {
      steps {
        container('kaniko') {
          sh '''
            /kaniko/executor \
              --context "${WORKSPACE}/${DOCKER_CONTEXT}" \
              --dockerfile "${WORKSPACE}/${DOCKER_CONTEXT}/Dockerfile" \
              --destination "${ACR_IMAGE}:${BUILD_NUMBER}" \
              --destination "${ACR_IMAGE}:latest" \
              --cache=true \
              --cache-repo="${ACR_IMAGE}-cache" \
              --digest-file=/tmp/digest.txt
            echo "Pushed ${ACR_IMAGE}:${BUILD_NUMBER}"
            echo "Digest: $(cat /tmp/digest.txt)"
          '''
        }
      }
    }
  }

  post {
    success { notify('Deployment succeeded!') }
    failure { notify('Deployment failed!') }
  }
}
```

### ACR push identity

Either option works:

```bash
# Option A: service principal with push rights on the registry
ACR_ID=$(az acr show --name <acrName> --query id -o tsv)
az ad sp create-for-rbac --name jenkins-acr-push --role AcrPush --scopes $ACR_ID

# Option B: repository-scoped token
az acr scope-map create --name jenkins-push-scope --registry <acrName> \
  --repository ordersapi content/read content/write \
  --repository ordersapi-cache content/read content/write
az acr token create --name jenkins-push --registry <acrName> --scope-map jenkins-push-scope
```

Notes:

- Remove `--insecure` and `--skip-tls-verify` for ACR; they are only for a local HTTP registry.
- If you use a scoped token, include the cache repository (`ordersapi-cache`) in its scope, or delete the two `--cache` lines.
- Verify the push: `az acr repository show-tags --name <acrName> --repository ordersapi -o table`.

## Pipeline: Push to AWS ECR

Same structure as the ACR pipeline. Differences: credentials come from a Jenkins credential (AWS keys), Kaniko uses the built-in `ecr-login` credential helper, and the ECR repository must already exist.

Replace the `environment` block and the build stage as follows; the pod spec is the same as ACR except the `docker-config` volume and its mount are not needed.

```groovy
  environment {
    AWS_REGION     = 'us-east-1'
    AWS_ACCOUNT_ID = '<aws-account-id>'
    ECR_REGISTRY   = "${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
    ECR_IMAGE      = "${ECR_REGISTRY}/microservices/orderapi"
    DOCKER_CONTEXT = 'Jenkins-Smart-Utils/Microservices/OrdersApi'
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
  }

  // ...

    stage('Docker Build & ECR Push') {
      steps {
        container('kaniko') {
          withCredentials([usernamePassword(
              credentialsId: 'aws-ecr-creds',
              usernameVariable: 'AWS_ACCESS_KEY_ID',
              passwordVariable: 'AWS_SECRET_ACCESS_KEY')]) {
            sh '''
              mkdir -p /kaniko/.docker
              echo "{\\"credHelpers\\":{\\"${ECR_REGISTRY}\\":\\"ecr-login\\"}}" > /kaniko/.docker/config.json

              /kaniko/executor \
                --context "${WORKSPACE}/${DOCKER_CONTEXT}" \
                --dockerfile "${WORKSPACE}/${DOCKER_CONTEXT}/Dockerfile" \
                --destination "${ECR_IMAGE}:${BUILD_NUMBER}" \
                --destination "${ECR_IMAGE}:latest"
            '''
          }
        }
      }
    }
```

Setup:

- Create a Jenkins credential of kind **Username with password**, ID `aws-ecr-creds` (username = access key ID, password = secret access key).
- Use a dedicated IAM user limited to ECR push on that repository: `ecr:GetAuthorizationToken`, `ecr:BatchCheckLayerAvailability`, `ecr:InitiateLayerUpload`, `ecr:UploadLayerPart`, `ecr:CompleteLayerUpload`, `ecr:PutImage`, `ecr:BatchGetImage`, `ecr:GetDownloadUrlForLayer`.
- Create the repository `microservices/orderapi` in the target region beforehand; Kaniko does not create it.

### ACR vs ECR at a glance

| | ECR | ACR |
|---|---|---|
| Credentials | Jenkins credential with AWS keys plus `ecr-login` helper config | Kubernetes secret mounted as `config.json` |
| Repository | Must exist before the first push | Created on first push |
| Auth step in the pipeline | Writes `config.json` at runtime | None; the mount handles it |

## AKS Setup Checklist (Corporate Jenkins)

The Jenkins team owns Jenkins; you own the AKS side. Work through these in order.

### A. Confirm with the Jenkins team first

- [ ] Where does Jenkins run (on-prem, VM, another cloud, inside Azure)? This decides the network path.
- [ ] Is the Kubernetes plugin installed, and can they add a new Kubernetes cloud?
- [ ] Do they support **WebSocket** agent connections, or only JNLP on port 50000?
- [ ] What is the Jenkins URL the pods must call back to, and does it use a corporate CA certificate?
- [ ] Which auth method will they accept for the cluster connection (service account token, kubeconfig, or Azure AD service principal)?
- [ ] Can pipelines define their own pod YAML, or must they use pod templates the Jenkins team defines centrally?

### B. Network

- [ ] Decide API server exposure: public endpoint with authorized IP ranges (allow the Jenkins controller), or a private cluster reachable through VPN, ExpressRoute or peering.
- [ ] Make sure agent pods can reach the Jenkins URL outbound.
- [ ] Make sure build pods can reach GitHub, NuGet feeds and the registry (check firewall, UDR and proxy rules).

### C. AKS cluster

- [ ] Create or choose the AKS cluster.
- [ ] Add a dedicated build node pool with autoscaler min 0, a label and a taint, so builds are isolated and cost nothing when idle:
  ```bash
  az aks nodepool add --cluster-name <aks> -g <rg> -n build \
    --node-count 1 --enable-cluster-autoscaler --min-count 0 --max-count 3 \
    --labels workload=build --node-taints build=true:NoSchedule
  ```
- [ ] Apply the namespace, quota and limits (`kubectl apply -k .`).
- [ ] Optionally apply the network policy (AKS network policy must be enabled).

### D. Access for Jenkins

- [ ] Apply `01-rbac.yaml` (ServiceAccount, Role, RoleBinding, token Secret).
- [ ] Collect the API server URL, cluster CA certificate, namespace and token.

### E. Registry

- [ ] Choose the registry and repository naming convention.
- [ ] Create the push identity (`AcrPush`) and the `acr-cred` secret in `jenkins-agents`.
- [ ] Attach ACR to AKS (`az aks update --attach-acr`) so the cluster can pull the images later.

### F. Build tooling

- [ ] Confirm the Kaniko executor, `jenkins/inbound-agent` and .NET SDK images are reachable. If public registries are blocked, mirror them into ACR and use those paths.
- [ ] In the pod template for AKS, add the node pool settings:
  ```yaml
  spec:
    nodeSelector:
      workload: build
    tolerations:
    - key: build
      operator: Equal
      value: "true"
      effect: NoSchedule
  ```

### G. GitHub

- [ ] Confirm the credential Jenkins uses to read your repository (GitHub App, PAT or deploy key).
- [ ] Confirm the webhook or branch-source trigger approach.
- [ ] Make sure the repository has a working `Dockerfile` and a `Jenkinsfile`.

### H. Validation

- [ ] Run a test pod manually in `jenkins-agents` with the Kaniko image to prove nodes, registry access and egress work.
- [ ] After the Jenkins team creates the cloud, run the smoke test pipeline, then the real build.

## Information to Share with the Jenkins Team

Send the token through a secure channel, not email or chat.

- AKS API server URL (and whether it is public or private)
- Cluster CA certificate
- Namespace: `jenkins-agents`
- ServiceAccount name (`jenkins`) and its token
- Node pool label and toleration (`workload=build`, `build=true:NoSchedule`)
- Whether the pipeline or the Jenkins team owns the pod template (and the template YAML if they own it)
- Registry name and the secret name used for pushes (`acr-cred`)
- Agent image paths, if you are mirroring them
- Repository URL and branch strategy

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `pods is forbidden ... in the namespace "jenkins-agent"` on Test Connection | Namespace typo. The cloud config must say `jenkins-agents` (with the "s"), matching the Role and RoleBinding. |
| `Failed to parse yaml ... could not find expected ':'` | Missing space in the pod YAML. Use `command: ["sleep"]`, not `command:["sleep"]`. |
| `sleep: invalid time interval 'infinty'` and the `dotnet` container exits | Typo in `args`. Use `args: ["infinity"]`. A fix in the wrong place also keeps the same commit being fetched, so edit the file that actually defines the pod template and push to the branch Jenkins loads. |
| Pod test shows `Connection refused` from `host.docker.internal` | Wrong port, or Jenkins published only on `127.0.0.1`. Use the host port from `docker ps` and publish on all interfaces (`-p 8080:8080`). |
| IPv6 `Network unreachable` lines in curl output | Harmless. Look for the IPv4 attempt and the final result. |
| `kubectl run -it` says it couldn't attach | The container exited before attach. Run without `-it` and read `kubectl logs`. |
| Pod stuck `Pending` or agent never connects | Wrong Jenkins URL in the cloud config (not `localhost`), WebSocket not ticked, or node selector/toleration mismatch. Use `kubectl describe pod -n jenkins-agents <pod>`. |
| Pod rejected with a quota error | ResourceQuota exceeds what Docker Desktop has. Lower the quota values or give Docker Desktop more resources. |
| Kaniko `OOMKilled` | Raise the Kaniko memory limit or give the cluster more memory. |
| Registry `UNAUTHORIZED` / `401` | Wrong username, password or server name in the secret, or the identity lacks `AcrPush`. Role assignments can take a few minutes to propagate. |
| Registry `insufficient_scope` / `denied` | Token scope does not include the repository, including the cache repository. |
| ECR `repository does not exist` | Create the repository first; Kaniko does not. |
| ECR `no basic auth credentials` | AWS credentials or the `ecr-login` `config.json` were not picked up. Check the credential ID and the generated `config.json`. |
| `dotnet: not found` in a library step | The step ran in the `jnlp` container. Call it inside `container('dotnet') { ... }`. |
| x509 / TLS errors to GitHub or the registry | Add the corporate CA to the agent image or mount it. |
| Timeouts reaching ACR | ACR firewall or private endpoint. AKS nodes must be on an allowed network or have a private-link route. |

## Cleanup

```bash
# Remove everything created for the agents
kubectl delete -k .
kubectl delete -f acr-secret.template.yaml --ignore-not-found
kubectl delete -f 03-networkpolicy.optional.yaml --ignore-not-found
```

Also delete the Jenkins credentials (`k8s-token`, and `aws-ecr-creds` if used), and revoke the registry push identity (service principal, token or IAM user) when it is no longer needed.