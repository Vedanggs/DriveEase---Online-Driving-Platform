// ─────────────────────────────────────────────────────────────────────────────
// Module: keyvault.bicep
// Provisions: Azure Key Vault (RBAC mode) + two secrets:
//   sql-connection-string  — MI-based SQL connection string, no password
//   servicebus-namespace   — Service Bus FQDN for DefaultAzureCredential
//
// Role assignments (Key Vault Secrets User → App Service MI) are wired in
// main.bicep after the App Service principal ID is known.
// ─────────────────────────────────────────────────────────────────────────────

@description('Key Vault name — globally unique, 3–24 alphanumeric/hyphens')
param kvName string

@description('Azure region')
param location string

@description('MI-compatible SQL connection string (no password — uses Active Directory Managed Identity)')
param sqlConnectionString string

@description('Service Bus fully-qualified namespace, e.g. myns.servicebus.windows.net')
param serviceBusNamespace string

// ── Key Vault ─────────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true   // Azure RBAC instead of legacy access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// ── Secrets ───────────────────────────────────────────────────────────────────
resource sqlConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-connection-string'
  properties: {
    value: sqlConnectionString
  }
}

resource sbNamespaceSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'servicebus-namespace'
  properties: {
    value: serviceBusNamespace
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output kvName string = keyVault.name
output kvUri  string = keyVault.properties.vaultUri
