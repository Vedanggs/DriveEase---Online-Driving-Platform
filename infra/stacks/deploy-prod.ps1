<#
.SYNOPSIS
  Deploys DriveEase PROD infrastructure via Azure Deployment Stacks.

.DESCRIPTION
  Uses `az stack group create` instead of a plain deployment so every resource
  is tracked, drift is detectable, and teardown is one command (teardown-prod.ps1).

  Run this BEFORE `azd deploy --environment prod`.
  SQL_ADMIN_PASSWORD MUST be set — no fallback in prod.

.PREREQUISITES
  az login
  azd auth login
  $env:SQL_ADMIN_PASSWORD = "<strong-prod-secret>"

.EXAMPLE
  .\infra\stacks\deploy-prod.ps1
#>

param(
  [string]$Location = 'eastus'
)

$ErrorActionPreference = 'Stop'

$stackName     = 'driveease-prod-stack'
$resourceGroup = 'rg-driveease-prod'
$templateFile  = 'infra/main.bicep'
$paramsFile    = 'infra/parameters/prod.bicepparam'

# Guard — SQL password must be set for prod
if (-not $env:SQL_ADMIN_PASSWORD) {
  Write-Error 'SQL_ADMIN_PASSWORD is not set. Set it before deploying to prod.'
  exit 1
}

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Magenta
Write-Host '  DriveEase — PROD Deployment Stack' -ForegroundColor Magenta
Write-Host '=====================================================' -ForegroundColor Magenta

# ── 1. Resource Group ─────────────────────────────────────────────────────────
Write-Host ''
Write-Host '[1/3] Ensuring resource group exists...' -ForegroundColor Yellow
az group create `
  --name     $resourceGroup `
  --location $Location `
  --output   table

# ── 2. Deployment Stack ───────────────────────────────────────────────────────
# --deny-settings-mode denyWriteAndDelete  → prevents manual drift on stack resources
# --action-on-unmanage deleteAll           → clean teardown removes everything
Write-Host ''
Write-Host "[2/3] Creating / updating stack '$stackName'..." -ForegroundColor Yellow
az stack group create `
  --name                  $stackName `
  --resource-group        $resourceGroup `
  --template-file         $templateFile `
  --parameters            $paramsFile `
  --deny-settings-mode    denyDelete `
  --action-on-unmanage    deleteAll `
  --yes `
  --output table

# ── 3. Show Outputs ───────────────────────────────────────────────────────────
Write-Host ''
Write-Host '[3/3] Stack outputs:' -ForegroundColor Yellow
az stack group show `
  --name           $stackName `
  --resource-group $resourceGroup `
  --query          '{apiUrl:outputs.apiUrl.value, sqlFqdn:outputs.sqlServerFqdn.value, serviceBus:outputs.serviceBusNamespace.value, keyVault:outputs.keyVaultName.value}' `
  --output         table

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Green
Write-Host '  PROD stack ready.' -ForegroundColor Green
Write-Host '  Next: azd deploy --environment prod' -ForegroundColor Green
Write-Host '=====================================================' -ForegroundColor Green
