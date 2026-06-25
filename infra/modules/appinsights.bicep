// ─────────────────────────────────────────────────────────────────────────────
// Module: appinsights.bicep
// Provisions: Log Analytics Workspace + workspace-based Application Insights.
//
// Workspace-based App Insights (vs classic) is the current recommendation:
//   - Data lands in a Log Analytics table — KQL queries work across resources.
//   - Retention and cost controls live on the workspace.
//   - Required for cross-resource correlation (e.g. API + worker + DB in one trace).
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region')
param location string

@description('Application Insights resource name')
param appInsightsName string

@description('Log Analytics Workspace resource name')
param workspaceName string

@description('Log retention in days — 30 is free tier, up to 730 for paid')
param retentionDays int = 30

// ── Log Analytics Workspace ───────────────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ── Application Insights (workspace-based) ───────────────────────────────────
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type:                'web'
    WorkspaceResourceId:             logAnalytics.id
    IngestionMode:                   'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery:     'Enabled'
    RetentionInDays:                 retentionDays
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
// connectionString is what the OTel SDK reads via APPLICATIONINSIGHTS_CONNECTION_STRING.
// instrumentationKey is the legacy value — kept for reference / older SDKs.
output connectionString    string = appInsights.properties.ConnectionString
output instrumentationKey  string = appInsights.properties.InstrumentationKey
output appInsightsName     string = appInsights.name
output workspaceName       string = logAnalytics.name
output workspaceId         string = logAnalytics.id
