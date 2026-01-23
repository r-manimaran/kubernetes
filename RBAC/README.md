RBAC in Kubernetes
This repository contains YAML configuration files to demonstrate Role-Based Access Control (RBAC) in Kubernetes. The configurations include the creation of a namespace, role, service account, and role binding.

Defintions:
RBAC:
Role-Based Access Control (RBAC) is a method of regulating access to computer or network resources based on the roles of individual users within an enterprise. In Kubernetes, RBAC is used to control access to the Kubernetes API and resources.

*Role*:
A Role in Kubernetes defines a set of permissions within a specific namespace. It specifies what actions can be performed on which resources.

*RoleBinding*:
A RoleBinding in Kubernetes grants the permissions defined in a Role to a user or set of users (including Service Accounts) within a specific namespace.

*Service Account*:
A Service Account in Kubernetes is an account for processes that run in a Pod. It provides an identity for processes that run in a Pod to interact with the Kubernetes API.

Files:
1.namespace.yaml - Creates a namespace named "dev".
2.role.yaml - Defines a Role named "backend-reader-role" in the "dev" namespace with permissions to read pods, services, and configmaps.
3.roleBinding.yaml - Binds the "backend-reader-role" to the "backend-reader-sa" Service Account in the "dev" namespace.
4.serviceAccount.yaml - Creates a Service Account named "backend-reader-sa" in the "dev" namespace.
Usage:
To apply these configurations to your Kubernetes cluster, use the following commands:
kubectl apply -f 1.namespace.yaml
kubectl apply -f 4.serviceAccount.yaml
kubectl apply -f 2.role.yaml
kubectl apply -f 3.roleBinding.yaml
Make sure to apply them in the order listed above to ensure that dependencies are met.
