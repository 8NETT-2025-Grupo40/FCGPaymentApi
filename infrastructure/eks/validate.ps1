# Script de validação do deployment EKS
# Verifica se Payment API está rodando corretamente

Write-Host "=== FCG Payment API - EKS Validation ===" -ForegroundColor Cyan
Write-Host ""

$namespace = "fcg"
$appName = "fcg-payment-api"

# Verificar se cluster está acessível
Write-Host "1. Verificando acesso ao cluster..." -ForegroundColor Yellow
kubectl cluster-info | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Cluster não acessível. Execute setup.ps1 primeiro." -ForegroundColor Red
    exit 1
}
Write-Host "✓ Cluster acessível" -ForegroundColor Green
Write-Host ""

# Verificar namespace
Write-Host "2. Verificando namespace '$namespace'..." -ForegroundColor Yellow
kubectl get namespace $namespace 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Namespace '$namespace' não existe" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Namespace existe" -ForegroundColor Green
Write-Host ""

# Verificar pods
Write-Host "3. Verificando pods do Payment API..." -ForegroundColor Yellow
$pods = kubectl get pods -n $namespace -l "app.kubernetes.io/name=$appName" -o json | ConvertFrom-Json
if ($pods.items.Count -eq 0) {
    Write-Host "✗ Nenhum pod encontrado" -ForegroundColor Red
    exit 1
}

$runningPods = $pods.items | Where-Object { $_.status.phase -eq "Running" }
Write-Host "  Total pods: $($pods.items.Count)" -ForegroundColor White
Write-Host "  Running: $($runningPods.Count)" -ForegroundColor White

if ($runningPods.Count -eq 0) {
    Write-Host "✗ Nenhum pod em execução" -ForegroundColor Red
    kubectl get pods -n $namespace -l "app.kubernetes.io/name=$appName"
    exit 1
}
Write-Host "✓ Pods rodando" -ForegroundColor Green
Write-Host ""

# Verificar External Secrets
Write-Host "4. Verificando External Secrets..." -ForegroundColor Yellow
kubectl get externalsecret -n $namespace 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    $externalSecret = kubectl get externalsecret -n $namespace -o json | ConvertFrom-Json
    $paymentSecret = $externalSecret.items | Where-Object { $_.metadata.name -like "*payment*" }
    if ($paymentSecret) {
        $status = $paymentSecret.status.conditions | Where-Object { $_.type -eq "Ready" }
        if ($status.status -eq "True") {
            Write-Host "✓ External Secrets sincronizado" -ForegroundColor Green
        } else {
            Write-Host "⚠ External Secrets não sincronizado" -ForegroundColor Yellow
            Write-Host "  Status: $($status.message)" -ForegroundColor Gray
        }
    }
}
Write-Host ""

# Verificar Services
Write-Host "5. Verificando Services..." -ForegroundColor Yellow
$services = kubectl get svc -n $namespace -l "app.kubernetes.io/name=$appName" -o json | ConvertFrom-Json
Write-Host "  Services encontrados: $($services.items.Count)" -ForegroundColor White
foreach ($svc in $services.items) {
    Write-Host "  - $($svc.metadata.name) ($($svc.spec.type))" -ForegroundColor Gray
}
Write-Host "✓ Services configurados" -ForegroundColor Green
Write-Host ""

# Verificar Ingress
Write-Host "6. Verificando Ingress (ALB)..." -ForegroundColor Yellow
$ingress = kubectl get ingress -n $namespace -o json | ConvertFrom-Json
$paymentIngress = $ingress.items | Where-Object { $_.metadata.name -like "*payment*" }
if ($paymentIngress) {
    $albUrl = $paymentIngress.status.loadBalancer.ingress[0].hostname
    if ($albUrl) {
        Write-Host "✓ ALB provisionado" -ForegroundColor Green
        Write-Host "  URL: http://$albUrl" -ForegroundColor Cyan
        Write-Host "  Paths:" -ForegroundColor White
        Write-Host "    - http://$albUrl/payments" -ForegroundColor Gray
        Write-Host "    - http://$albUrl/psp" -ForegroundColor Gray
    } else {
        Write-Host "⚠ ALB ainda sendo provisionado (pode levar 2-3 minutos)" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Ingress não encontrado" -ForegroundColor Red
}
Write-Host ""

# Verificar HPA
Write-Host "7. Verificando HPA (autoscaling)..." -ForegroundColor Yellow
kubectl get hpa -n $namespace -l "app.kubernetes.io/name=$appName" 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ HPA configurado" -ForegroundColor Green
    kubectl get hpa -n $namespace -l "app.kubernetes.io/name=$appName"
} else {
    Write-Host "⚠ HPA não encontrado" -ForegroundColor Yellow
}
Write-Host ""

# Testar health endpoint
if ($albUrl) {
    Write-Host "8. Testando health endpoint..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://$albUrl/payments/health" -Method Get -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ Health endpoint respondendo (200 OK)" -ForegroundColor Green
        } else {
            Write-Host "⚠ Health endpoint retornou: $($response.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "✗ Erro ao acessar health endpoint: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "=== Validação concluída ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para logs dos pods:" -ForegroundColor White
Write-Host "  kubectl logs -n $namespace -l app.kubernetes.io/name=$appName --all-containers=true --tail=50" -ForegroundColor Gray
Write-Host ""
Write-Host "Para descrever problemas:" -ForegroundColor White
Write-Host "  kubectl describe pod -n $namespace -l app.kubernetes.io/name=$appName" -ForegroundColor Gray
Write-Host ""
