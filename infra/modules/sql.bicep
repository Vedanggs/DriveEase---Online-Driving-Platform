// ─────────────────────────────────────────────────────────────────────────────
// Module: sql.bicep
// Provisions: Azure SQL Server + Database + firewall rule for Azure services.
//
// Day 25: admin credentials are used only to provision the server.
//         The app connects via Managed Identity — no password in the
//         connection string that reaches the app.
// ─────────────────────────────────────────────────────────────────────────────

@description('SQL Server resource name — must be globally unique')
param serverName string

@description('Database name')
param databaseName string

@description('Azure region')
param location string

@description('SQL administrator login')
param adminLogin string

@secure()
@description('SQL administrator password (min 8 chars, upper + lower + digit + special)')
param adminPassword string

@description('Database SKU name — Basic | S1 | S2 | P1 | …')
param skuName string = 'Basic'

@description('Database SKU service tier — Basic | Standard | Premium')
param skuTier string = 'Basic'

@description('DTU capacity matching the chosen tier')
param skuCapacity int = 5

@description('Object ID of the App Service MI to set as the AAD administrator')
param appServicePrincipalId string = ''

@description('Display name for the AAD administrator (typically the App Service name)')
param appServiceName string = ''

// ── SQL Server ────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin:         adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion:          '1.2'
    publicNetworkAccess:        'Enabled'
  }
}

// ── Database ──────────────────────────────────────────────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name:     skuName
    tier:     skuTier
    capacity: skuCapacity
  }
  properties: {
    collation:    'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: skuTier == 'Basic' ? 2147483648 : 21474836480
    requestedBackupStorageRedundancy: 'Local'
  }
}

// Allow inbound connections from Azure-hosted services (e.g. App Service)
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress:   '0.0.0.0'
  }
}

// Set the App Service MI as the SQL Server Azure AD administrator so it can
// authenticate with Active Directory Managed Identity — no password required.
resource sqlAadAdmin 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = if (!empty(appServicePrincipalId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login:             appServiceName
    sid:               appServicePrincipalId
    tenantId:          subscription().tenantId
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output serverId   string = sqlServer.id

// MI-compatible connection string — no User/Password.
// Authentication=Active Directory Managed Identity tells Microsoft.Data.SqlClient
// to request an OAuth token for the App Service system-assigned identity.
output miConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
