// ─────────────────────────────────────────────────────────────────────────────
// Module: sql.bicep
// Provisions: Azure SQL Server + Database + firewall rule for Azure services
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
    // 2 GB for Basic, 20 GB for Standard/Premium
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

// ── Outputs ───────────────────────────────────────────────────────────────────
// @secure() prevents the connection string from appearing in deployment history
@secure()
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User Id=${adminLogin};Password=${adminPassword};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;'

output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output serverId   string = sqlServer.id
