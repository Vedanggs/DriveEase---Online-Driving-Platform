targetScope = 'resourceGroup'

// ─────────────────────────────────────────────────────────────────────────────
// DriveEase — main orchestration template
//
// Deployed via Azure Deployment Stacks (not plain `az deployment group create`).
// Use the scripts in infra/stacks/ — they call `az stack group create` which
// tracks every resource, detects drift, and enables clean one-command teardown.
//
// DEV stack  : .\infra\stacks\deploy-dev.ps1
// PROD stack : .\infra\stacks\deploy-prod.ps1
// App code   : azd deploy --environment <dev|prod>   (after stack is up)
// Teardown   : .\infra\stacks\teardown-dev.ps1
//
// Day 25 identity chain:
//   App Service MI ──RBAC──▶ Key Vault Secrets User
//   App Service MI ──RBAC──▶ Azure Service Bus Data Owner
//   App Service MI ──AAD ──▶ SQL Server AAD administrator
//   App Settings   ──────── @Microsoft.KeyVault() references only (zero plaintext secrets)
//
// Day 27 network chain (prod only):
//   App Service (app-subnet) ──Private Endpoint──▶ Azure SQL Server (data-subnet)
//   App Service (app-subnet) ──Private Endpoint──▶ Azure Service Bus Premium (data-subnet)
// ─────────────────────────────────────────────────────────────────────────────

@description('Environment name — drives SKU selection and resource naming')
@allowed(['dev', 'prod'])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL Server administrator login')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

// Stable 8-char suffix derived from subscription + RG — identical on every re-deploy
var suffix = take(uniqueString(subscription().id, resourceGroup().name), 8)
var prefix  = 'driveease-${environmentName}'

// Key Vault name must be ≤24 chars — truncate to fit
var kvName = take('${prefix}-kv-${suffix}', 24)

// In prod the data tier is network-isolated behind private endpoints.
// dev uses public access so developers can connect without VNet tooling.
var isProd = environmentName == 'prod'

// ── Network (VNet + subnets) ──────────────────────────────────────────────────
// Always provisioned — dev uses the VNet for App Service integration readiness;
// private endpoints are only wired in prod via the subnetId params below.
module network 'modules/network.bicep' = {
  name: 'deploy-network'
  params: {
    vnetName: '${prefix}-vnet-${suffix}'
    location: location
  }
}

// ── SQL ───────────────────────────────────────────────────────────────────────
module sql 'modules/sql.bicep' = {
  name: 'deploy-sql'
  params: {
    serverName:   '${prefix}-sql-${suffix}'
    databaseName: 'driveease'
    location:      location
    adminLogin:    sqlAdminLogin
    adminPassword: sqlAdminPassword
    skuName:      isProd ? 'S2'       : 'Basic'
    skuTier:      isProd ? 'Standard' : 'Basic'
    skuCapacity:  isProd ? 50         : 5
    // Private endpoint: prod only — disables public access and creates endpoint in data-subnet
    subnetId: isProd ? network.outputs.dataSubnetId : ''
    vnetId:   isProd ? network.outputs.vnetId       : ''
  }
}

// ── Service Bus ───────────────────────────────────────────────────────────────
module serviceBus 'modules/servicebus.bicep' = {
  name: 'deploy-servicebus'
  params: {
    namespaceName: '${prefix}-sb-${suffix}'
    location:       location
    // Premium SKU required for private endpoints; Standard used in dev (cheaper)
    skuName:  isProd ? 'Premium'  : 'Standard'
    subnetId: isProd ? network.outputs.dataSubnetId : ''
    vnetId:   isProd ? network.outputs.vnetId       : ''
  }
}

// ── Key Vault ─────────────────────────────────────────────────────────────────
module keyVault 'modules/keyvault.bicep' = {
  name: 'deploy-keyvault'
  params: {
    kvName:               kvName
    location:             location
    sqlConnectionString:  sql.outputs.miConnectionString
    serviceBusNamespace:  serviceBus.outputs.namespaceFqdn
  }
}

// ── App Insights ──────────────────────────────────────────────────────────────
module appInsights 'modules/appinsights.bicep' = {
  name: 'deploy-appinsights'
  params: {
    appInsightsName: '${prefix}-ai-${suffix}'
    workspaceName:   '${prefix}-law-${suffix}'
    location:         location
    retentionDays:   isProd ? 90 : 30
  }
}

// ── API ───────────────────────────────────────────────────────────────────────
module api 'modules/api.bicep' = {
  name: 'deploy-api'
  params: {
    appName:                     '${prefix}-api-${suffix}'
    location:                     location
    planSku:                     isProd ? 'S1' : 'B1'
    keyVaultName:                keyVault.outputs.kvName
    environmentName:              environmentName
    aadTenantId:                 'bd95d6a2-b815-456f-872e-947582249315'
    aadClientId:                 'a6890318-c857-4b50-ae3f-eb3fbfd1cc81'
    appInsightsConnectionString: appInsights.outputs.connectionString
  }
}

// ── Identity wiring ───────────────────────────────────────────────────────────
var kvSecretsUserRoleId   = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
var sbDataOwnerRoleId     = '090c5cfd-751d-490a-894a-3ce6f1109419' // Azure Service Bus Data Owner

resource kvRef 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: kvName
}

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kvRef
  name: guid(kvName, '${prefix}-api-${suffix}', kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId:      api.outputs.principalId
    principalType:    'ServicePrincipal'
  }
}

resource sbRef 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: '${prefix}-sb-${suffix}'
}

resource sbRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sbRef
  name: guid('${prefix}-sb-${suffix}', '${prefix}-api-${suffix}', sbDataOwnerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbDataOwnerRoleId)
    principalId:      api.outputs.principalId
    principalType:    'ServicePrincipal'
  }
}

resource sqlServerRef 'Microsoft.Sql/servers@2023-08-01-preview' existing = {
  name: '${prefix}-sql-${suffix}'
}

#disable-next-line no-unnecessary-dependson
resource sqlAadAdmin 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = {
  parent: sqlServerRef
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login:             '${prefix}-api-${suffix}'
    sid:               api.outputs.principalId
    tenantId:          subscription().tenantId
  }
  dependsOn: [sql, api]
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output apiUrl              string = api.outputs.appUrl
output sqlServerFqdn       string = sql.outputs.serverFqdn
output serviceBusNamespace string = serviceBus.outputs.namespaceName
output keyVaultName        string = keyVault.outputs.kvName
output appInsightsName     string = appInsights.outputs.appInsightsName
output logAnalyticsWorkspace string = appInsights.outputs.workspaceName
output vnetName            string = network.outputs.vnetId
