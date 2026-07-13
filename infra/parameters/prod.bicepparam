// Production environment parameter file
// Usage: az deployment group create -g rg-driveease-prod \
//            --template-file infra/main.bicep \
//            --parameters infra/parameters/prod.bicepparam
//
// SQL_ADMIN_PASSWORD MUST be set in the pipeline / CD environment — no fallback.
// In CI/CD: store in GitHub Actions Secret / Azure Key Vault and inject as env var.
using '../main.bicep'

param environmentName  = 'prod'
param location         = 'centralindia'      // swap to paired region (e.g. westus2) for geo-HA
param sqlAdminLogin    = 'sqladmin'
// No fallback on purpose — deployment fails fast if SQL_ADMIN_PASSWORD isn't set,
// so a real prod secret can never be baked into source control.
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
