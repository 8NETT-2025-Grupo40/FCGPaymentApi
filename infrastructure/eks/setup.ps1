# Script de setup do EKS cluster
# Reutiliza o cluster "fcg" criado pela User API
# Este script documenta os comandos necessários caso o cluster não exista

Write-Host "=== FCG Payment API - EKS Setup ===" -ForegroundColor Cyan
Write-Host ""

# Verificar se o cluster já existe
Write-Host "Verificando se cluster EKS 'fcg' já existe..." -ForegroundColor Yellow
$clusterExists = aws eks describe-cluster --name fcg --region us-east-1 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Cluster 'fcg' já existe (criado pela User API)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Configurando kubectl para acessar o cluster..." -ForegroundColor Yellow
    aws eks update-kubeconfig --region us-east-1 --name fcg
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ kubectl configurado com sucesso" -ForegroundColor Green
        Write-Host ""
        kubectl cluster-info
    } else {
        Write-Host "✗ Erro ao configurar kubectl" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✗ Cluster 'fcg' não existe" -ForegroundColor Red
    Write-Host ""
    Write-Host "O cluster deve ser criado primeiro pela User API usando:" -ForegroundColor Yellow
    Write-Host "  eksctl create cluster -f infrastructure/eks/cluster-config.yaml" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Referência: FCGUserApi PR #11 - kubernetes branch" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "=== Próximos passos ===" -ForegroundColor Cyan
Write-Host "1. Criar IAM Policy para Payment API:" -ForegroundColor White
Write-Host "   aws iam create-policy --policy-name FCGPaymentAPIPolicy --policy-document file://infrastructure/iam/payment-api-policy.json" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Deploy via GitHub Actions ou manualmente:" -ForegroundColor White
Write-Host "   helm upgrade --install payment-api ./k8s --namespace fcg --create-namespace" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Verificar deployment:" -ForegroundColor White
Write-Host "   kubectl get pods -n fcg -l app.kubernetes.io/name=fcg-payment-api" -ForegroundColor Gray
Write-Host ""
