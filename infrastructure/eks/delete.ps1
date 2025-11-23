# Script de cleanup do Payment API no EKS
# Remove apenas o deployment do Payment API, mantém o cluster compartilhado

Write-Host "=== FCG Payment API - EKS Cleanup ===" -ForegroundColor Cyan
Write-Host ""

$namespace = "fcg"
$releaseName = "payment-api"

Write-Host "⚠ ATENÇÃO: Este script remove apenas o Payment API" -ForegroundColor Yellow
Write-Host "O cluster EKS 'fcg' será mantido (compartilhado com outras APIs)" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "Deseja continuar? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Operação cancelada." -ForegroundColor Gray
    exit 0
}

Write-Host ""
Write-Host "1. Removendo Helm release '$releaseName'..." -ForegroundColor Yellow
helm uninstall $releaseName --namespace $namespace 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Helm release removido" -ForegroundColor Green
} else {
    Write-Host "⚠ Helm release não encontrado ou já removido" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "2. Removendo recursos Kubernetes restantes..." -ForegroundColor Yellow
kubectl delete externalsecret -n $namespace -l "app.kubernetes.io/name=fcg-payment-api" 2>$null
kubectl delete secretstore -n $namespace -l "app.kubernetes.io/name=fcg-payment-api" 2>$null
kubectl delete secret -n $namespace -l "app.kubernetes.io/name=fcg-payment-api" 2>$null
Write-Host "✓ Recursos limpos" -ForegroundColor Green
Write-Host ""

Write-Host "3. Verificando IRSA (ServiceAccount)..." -ForegroundColor Yellow
$serviceAccountExists = kubectl get serviceaccount fcg-payment-api-sa -n $namespace 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "⚠ ServiceAccount 'fcg-payment-api-sa' ainda existe" -ForegroundColor Yellow
    Write-Host "  Para remover IRSA completamente, execute:" -ForegroundColor White
    Write-Host "  eksctl delete iamserviceaccount --cluster=fcg --namespace=$namespace --name=fcg-payment-api-sa --region=us-east-1" -ForegroundColor Gray
} else {
    Write-Host "✓ ServiceAccount já removido" -ForegroundColor Green
}
Write-Host ""

Write-Host "4. Verificando IAM Policy..." -ForegroundColor Yellow
Write-Host "⚠ IAM Policy 'FCGPaymentAPIPolicy' pode ainda existir" -ForegroundColor Yellow
Write-Host "  Para remover manualmente:" -ForegroundColor White
Write-Host "  aws iam delete-policy --policy-arn arn:aws:iam::478511033947:policy/FCGPaymentAPIPolicy" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Cleanup concluído ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Payment API removido do EKS." -ForegroundColor Green
Write-Host "Cluster 'fcg' e outras APIs permanecem intactos." -ForegroundColor White
Write-Host ""
