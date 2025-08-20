#!/bin/bash

#Usage: ./connect-to-aks.sh <your-resource-group> <your-aks-cluster> [<subscription-id>]

# Connect to AKS
az aks get-credentials --resource-group <your-resource-group> --name <your-aks-cluster> [<subscription-id>]

RESOURCE_GROUP=$1
CLUSTER_NAME=$2
SUBSCRIPTION_ID=$3

# Check inputs
if [[ -z $RESOURCE_GROUP || -z $CLUSTER_NAME ]]; then
    echo "Resource group name and AKS cluster name must be provided"
    exit 1
fi

echo "Logging in to Azure..."
az account show > /dev/null 2>&1 || az login

if [[ -n $SUBSCRIPTION_ID ]]; then
    echo "Setting subscription to $SUBSCRIPTION_ID"
    az account set --subscription $SUBSCRIPTION_ID
fi

echo "Getting credentials for cluster '$CLUSTER_NAME' in resource group '$RESOURCE_GROUP'..."
az aks get-credentials --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --overwrite-existing

echo "Switched Context to:"
kubectl config current-context

echo "Verifying connection to AKS..."
kubectl get nodes