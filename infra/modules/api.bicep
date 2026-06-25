// ─────────────────────────────────────────────────────────────────────────────
// Module: api.bicep
// Provisions: Linux App Service Plan + Web App (.NET 10) with system-assigned
//             managed identity, HTTPS-only.
//
// Day 25: ALL secrets removed from app settings.
//         Connection strings and Service Bus namespace are Key Vault references
//         — the App Service MI resolves them at runtime, no plaintext anywhere.
// ─────────────────────────────────────────────────────────────────────────────

@description('App Service name — must be globally unique')
param appName string

@description('Azure region')
param location string

@description('App Service Plan SKU — B1 (dev) | S1 (prod)')
param planSku string = 'B1'

@description('Key Vault name — used to build @Microsoft.KeyVault() reference strings')
param keyVaultName string

@description('Environment name — sets ASPNETCORE_ENVIRONMENT app setting')
param environmentName string

@description('Entra ID tenant ID — not a secret, safe in app settings')
param aadTenantId string

@description('Entra ID app registration client ID — not a secret, safe in app settings')
param aadClientId string

@description('Application Insights connection string — injected from the appinsights module output')
param appInsightsConnectionString string

// ── App Service Plan (Linux) ──────────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: planSku
  }
  properties: {
    reserved: true
  }
}

// ── Web App (.NET 10 on Linux) ────────────────────────────────────────────────
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  tags: {
    'azd-env-name': environmentName
    'azd-service-name': 'api'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly:    true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion:  '1.2'
      ftpsState:      'Disabled'
      http20Enabled:  true
      alwaysOn:       planSku != 'F1'
      appSettings: [
        {
          name:  'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
        // Key Vault reference — App Service MI resolves this at runtime.
        // No SAS key or plaintext here.
        {
          name:  'ServiceBus__FullyQualifiedNamespace'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=servicebus-namespace)'
        }
        // Entra ID config — these are non-secret public identifiers, safe in app settings.
        {
          name:  'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name:  'AzureAd__TenantId'
          value: aadTenantId
        }
        {
          name:  'AzureAd__ClientId'
          value: aadClientId
        }
        {
          name:  'AzureAd__Audience'
          value: 'api://${aadClientId}'
        }
        // OTel SDK reads APPLICATIONINSIGHTS_CONNECTION_STRING automatically.
        // This is the standard env-var name; no code change needed when rotating.
        {
          name:  'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
      ]
      connectionStrings: [
        // Key Vault reference — MI-based connection string, no password.
        {
          name:             'DefaultConnection'
          connectionString: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=sql-connection-string)'
          type:             'SQLAzure'
        }
      ]
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output appUrl      string = 'https://${appService.properties.defaultHostName}'
output appName     string = appService.name
output principalId string = appService.identity.principalId
