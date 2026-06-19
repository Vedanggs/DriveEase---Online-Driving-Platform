// ─────────────────────────────────────────────────────────────────────────────
// Module: sql.bicep
// Provisions: Azure SQL Server + Database.
//
// Day 25: admin credentials used only to provision the server.
//         App connects via Managed Identity — no password in the connection string.
//
// Day 27 security pass: when subnetId is provided (prod), public network access
//         is disabled and a private endpoint is created in the data subnet.
//         The AllowAzureServices firewall rule is only present in dev (no VNet).
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

@description('Subnet resource ID for the private endpoint. Empty = dev mode (public access retained).')
param subnetId string = ''

@description('VNet resource ID for the private DNS zone link. Required when subnetId is set.')
param vnetId string = ''

// Private endpoint enabled when a subnet is provided (prod)
var enablePrivateEndpoint = !empty(subnetId)

// ── SQL Server ────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin:         adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion:          '1.2'
    // Disable public access in prod; retain in dev where there is no VNet
    publicNetworkAccess:        enablePrivateEndpoint ? 'Disabled' : 'Enabled'
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

// Dev only: allow inbound from Azure-hosted services when no private endpoint is used
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (!enablePrivateEndpoint) {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress:   '0.0.0.0'
  }
}

// Set the App Service MI as the SQL Server Azure AD administrator
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

// ── Private Endpoint (prod only) ──────────────────────────────────────────────

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint) {
  name: '${serverName}-pe'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${serverName}-plsc'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
        }
      }
    ]
  }
}

resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = if (enablePrivateEndpoint) {
  name: 'privatelink.database.windows.net'
  location: 'global'
}

resource sqlDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = if (enablePrivateEndpoint) {
  parent: sqlPrivateDnsZone
  name: '${serverName}-vnet-link'
  location: 'global'
  properties: {
    virtualNetwork: { id: vnetId }
    registrationEnabled: false
  }
}

resource sqlDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint) {
  parent: sqlPrivateEndpoint
  name: 'sqlDnsZoneGroup'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-database-windows-net'
        properties: {
          privateDnsZoneId: sqlPrivateDnsZone.id
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output serverId   string = sqlServer.id

output miConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
