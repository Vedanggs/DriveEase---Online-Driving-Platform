// ─────────────────────────────────────────────────────────────────────────────
// Module: api.bicep
// Provisions: Linux App Service Plan + Web App (.NET 10) with system-assigned
//             managed identity, HTTPS-only, and injected connection strings.
// ─────────────────────────────────────────────────────────────────────────────

@description('App Service name — must be globally unique')
param appName string

@description('Azure region')
param location string

@description('App Service Plan SKU — B1 (dev) | P2v3 (prod)')
param planSku string = 'B1'

@secure()
@description('SQL Server connection string (injected into connection string slot)')
param sqlConnectionString string

@secure()
@description('Service Bus primary connection string')
param serviceBusConnectionString string

@description('Environment name — sets ASPNETCORE_ENVIRONMENT app setting')
param environmentName string

// ── App Service Plan (Linux) ──────────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: planSku
  }
  properties: {
    reserved: true // required for Linux plans
  }
}

// ── Web App (.NET 10 on Linux) ────────────────────────────────────────────────
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned' // enables passwordless access to Key Vault / managed resources
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly:    true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion:  '1.2'
      ftpsState:      'Disabled'
      http20Enabled:  true
      alwaysOn:       planSku != 'F1' // keep warm; Free tier doesn't support always-on
      appSettings: [
        {
          name:  'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
        {
          name:  'ServiceBus__ConnectionString'
          value: serviceBusConnectionString
        }
      ]
      connectionStrings: [
        {
          name:             'DefaultConnection'
          connectionString: sqlConnectionString
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
