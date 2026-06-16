// ─────────────────────────────────────────────────────────────────────────────
// Module: servicebus.bicep
// Provisions: Azure Service Bus Namespace + topics + subscriptions
//
// DriveEase async event flows wired here:
//   enrollment-events  → enrollment-confirmed, enrollment-alert
//   lesson-events      → lesson-reminder, lesson-completed
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
    disableLocalAuth:    false
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

// ── Authorization rule for application (Send + Listen) ───────────────────────
resource appAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2022-10-01-preview' = {
  parent: namespace
  name: 'DriveEaseApp'
  properties: {
    rights: ['Send', 'Listen']
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
@secure()
output primaryConnectionString string = appAuthRule.listKeys().primaryConnectionString
output namespaceName           string = namespace.name
output namespaceId             string = namespace.id
