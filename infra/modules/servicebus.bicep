// ─────────────────────────────────────────────────────────────────────────────
// Module: servicebus.bicep
// Provisions: Azure Service Bus Namespace + topics + subscriptions.
//
// Day 25: disableLocalAuth = true enforces that only Azure RBAC tokens
//         (Managed Identity) can send/receive — no SAS connection strings.
//
// Day 27 security pass: when subnetId is provided (prod), public network access
//         is disabled, the SKU is upgraded to Premium (required for private
//         endpoints), and a private endpoint is created in the data subnet.
// ─────────────────────────────────────────────────────────────────────────────

@description('Service Bus namespace name — must be globally unique')
param namespaceName string

@description('Azure region')
param location string

@description('Service Bus SKU — Standard (dev) | Premium (prod, required for private endpoints)')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Standard'

@description('Subnet resource ID for the private endpoint. Empty = dev mode (public access retained).')
param subnetId string = ''

@description('VNet resource ID for the private DNS zone link. Required when subnetId is set.')
param vnetId string = ''

var enablePrivateEndpoint = !empty(subnetId)

// ── Namespace ─────────────────────────────────────────────────────────────────
resource namespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    minimumTlsVersion:   '1.2'
    publicNetworkAccess: enablePrivateEndpoint ? 'Disabled' : 'Enabled'
    disableLocalAuth:    true    // SAS keys disabled — MI/RBAC only
  }
}

// ── Topics (Standard/Premium only — Basic has no pub/sub) ────────────────────
resource enrollmentEventsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: namespace
  name: 'enrollment-events'
  properties: {
    defaultMessageTimeToLive:   'P7D'
    enableBatchedOperations:    true
    requiresDuplicateDetection: false
  }
}

resource lessonEventsTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: namespace
  name: 'lesson-events'
  properties: {
    defaultMessageTimeToLive:   'P7D'
    enableBatchedOperations:    true
    requiresDuplicateDetection: false
  }
}

// ── Subscriptions — enrollment-events ─────────────────────────────────────────
resource enrollmentConfirmedSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: enrollmentEventsTopic
  name: 'enrollment-confirmed'
  properties: {
    lockDuration:                     'PT1M'
    defaultMessageTimeToLive:         'P7D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount:                 5
  }
}

resource enrollmentAlertSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: enrollmentEventsTopic
  name: 'enrollment-alert'
  properties: {
    lockDuration:                     'PT1M'
    defaultMessageTimeToLive:         'P7D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount:                 5
  }
}

// ── Subscriptions — lesson-events ─────────────────────────────────────────────
resource lessonReminderSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: lessonEventsTopic
  name: 'lesson-reminder'
  properties: {
    lockDuration:                     'PT1M'
    defaultMessageTimeToLive:         'P7D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount:                 5
  }
}

resource lessonCompletedSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (skuName != 'Basic') {
  parent: lessonEventsTopic
  name: 'lesson-completed'
  properties: {
    lockDuration:                     'PT1M'
    defaultMessageTimeToLive:         'P7D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount:                 5
  }
}

// ── Private Endpoint (prod / Premium only) ────────────────────────────────────

resource sbPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint) {
  name: '${namespaceName}-pe'
  location: location
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${namespaceName}-plsc'
        properties: {
          privateLinkServiceId: namespace.id
          groupIds: ['namespace']
        }
      }
    ]
  }
}

resource sbPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = if (enablePrivateEndpoint) {
  name: 'privatelink.servicebus.windows.net'
  location: 'global'
}

resource sbDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = if (enablePrivateEndpoint) {
  parent: sbPrivateDnsZone
  name: '${namespaceName}-vnet-link'
  location: 'global'
  properties: {
    virtualNetwork: { id: vnetId }
    registrationEnabled: false
  }
}

resource sbDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint) {
  parent: sbPrivateEndpoint
  name: 'sbDnsZoneGroup'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-servicebus-windows-net'
        properties: {
          privateDnsZoneId: sbPrivateDnsZone.id
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output namespaceName string = namespace.name
output namespaceId   string = namespace.id
output namespaceFqdn string = '${namespace.name}.servicebus.windows.net'
