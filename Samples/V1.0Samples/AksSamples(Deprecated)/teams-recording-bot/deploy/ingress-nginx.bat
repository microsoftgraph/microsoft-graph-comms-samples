@echo off

echo Creating ingress-nginx namespace
kubectl create namespace ingress-nginx

echo Adding helm repositories
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo add stable https://kubernetes-charts.storage.googleapis.com/
helm repo update

echo Installing ingress-nginx
helm install nginx-ingress ingress-nginx/ingress-nginx ^
    --create-namespace ^
    --namespace ingress-nginx ^
    --set controller.replicaCount=1 ^
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux ^
    --set controller.service.enabled=false ^
    --set controller.admissionWebhooks.enabled=false ^
    --set controller.config.log-format-stream="" ^
    --set controller.extraArgs.tcp-services-configmap=ingress-nginx/teams-recording-bot-tcp-services
