@echo off

echo Creating cert-manager namespace
kubectl create ns cert-manager

echo Updating helm repo
helm repo add jetstack https://charts.jetstack.io
helm repo update

echo Installing cert-manager
helm install ^
  cert-manager jetstack/cert-manager ^
  --namespace cert-manager ^
  --version v0.15.1 ^
  --set nodeSelector."beta\.kubernetes\.io/os"=linux ^
  --set webhook.nodeSelector."beta\.kubernetes\.io/os"=linux ^
  --set cainjector.nodeSelector."beta\.kubernetes\.io/os"=linux ^
  --set installCRDs=true

echo Waiting for cert-manager to be ready
kubectl wait pod -n cert-manager --for condition=ready --timeout=60s --all

echo Installing cluster issuer
kubectl apply -f cluster-issuer.yaml
