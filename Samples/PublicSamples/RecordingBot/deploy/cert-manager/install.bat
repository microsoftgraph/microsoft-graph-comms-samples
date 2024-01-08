@echo off

echo Updating helm repo
helm repo add jetstack https://charts.jetstack.io
helm repo update

echo Installing cert-manager
helm upgrade ^
  cert-manager jetstack/cert-manager ^
  --namespace cert-manager ^
  --create-namespace ^
  --version v1.13.3 ^
  --install ^
  --set nodeSelector."kubernetes\.io/os"=linux ^
  --set webhook.nodeSelector."kubernetes\.io/os"=linux ^
  --set cainjector.nodeSelector."kubernetes\.io/os"=linux ^
  --set installCRDs=true

echo Waiting for cert-manager to be ready
kubectl wait pod -n cert-manager --for condition=ready --timeout=60s --all

echo Installing cluster issuer
kubectl apply -f cluster-issuer.yaml