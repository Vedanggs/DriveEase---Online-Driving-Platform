// ─────────────────────────────────────────────────────────────────────────────
// Module: servicebus.bicep
// Provisions: Azure Service Bus Namespace + topics + subscriptions.
//
// Day 25: disableLocalAuth = true enforces that only Azure RBAC tokens
//         (Managed Identity) can send/receive — no SAS connection strings.
//         The RBAC role assignment (Azure Service Bus Data Owner → App Service MI)
//         is wired in main.bicep after the App Service principal ID is known.
// ─────────────────────────────────────────────────────────────────────────────

@description('Service Bus namespace name — must be globally unique')
param namespaceName string

@description('Azure region')
param location string

@description('Service Bus SKU — Basic (queues only) | Standard | Premium')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Standard'

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
    publicNetworkAccess: 'Enabled'
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

// ── Outputs ───────────────────────────────────────────────────────────────────
// No connection string output — local auth is disabled.
// App connects via DefaultAzureCredential using the FQDN below.
output namespaceName string = namespace.name
output namespaceId   string = namespace.id
output namespaceFqdn string = '${namespace.name}.servicebus.windows.net'
