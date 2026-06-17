<#
.SYNOPSIS
  Deploys DriveEase DEV infrastructure via Azure Deployment Stacks.

.DESCRIPTION
  Uses `az stack group create` instead of a plain deployment so every resource
  is tracked, drift is detectable, and teardown is one command (teardown-dev.ps1).

  Run this BEFORE `azd deploy --environment dev`.

.PREREQUISITES
  az login
  azd auth login
  $env:SQL_ADMIN_PASSWORD = "Dev@P@ssw0rd2025!"

.EXAMPLE
  .\infra\stacks\deploy-dev.ps1
#>

param(
  [string]$Location = 'eastus'
)

$ErrorActionPreference = 'Stop'

$stackName     = 'driveease-dev-stack'
$resourceGroup = 'rg-driveease-dev'
$templateFile  = 'infra/main.bicep'
$paramsFile    = 'infra/parameters/dev.bicepparam'

Write-Host ''
Write-Host '=====================================================' -ForegroundColor Cyan
Write-Host '  DriveEase — DEV Deployment Stack' -ForegroundColor Cyan
Write-Host '=====================================================' -ForegroundColor Cyan

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
Write-Host '  DEV stack ready.' -ForegroundColor Green
Write-Host '  Next: azd deploy --environment dev' -ForegroundColor Green
Write-Host '=====================================================' -ForegroundColor Green
