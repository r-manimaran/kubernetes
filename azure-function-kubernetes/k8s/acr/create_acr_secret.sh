#!/bin/bash

# Set ACR name
ACR_NAME="maranacr"

# GET ACR PASSWORD and create secret
ACR_PASSWORD=$(az acr credential show --name  $ACR_NAME --query "passwords[0].value" --output tsv )

# Create secret
kubectl create secret docker-registry acr-secret \
    --docker-server=$ACR_NAME.azurecr.io \
    --docker-username=$ACR_NAME \
    --docker-password="$ACR_PASSWORD" \
    
```
kubectl get pods -n logging
kubectl describe pod <pod-name> -n logging