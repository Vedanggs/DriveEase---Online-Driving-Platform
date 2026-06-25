// Dev environment parameter file
// Usage: az deployment group what-if/create -g rg-driveease-dev \
//            --template-file infra/main.bicep \
//            --parameters infra/parameters/dev.bicepparam
//
// Set SQL_ADMIN_PASSWORD before deploying:
//   $env:SQL_ADMIN_PASSWORD = "Dev@P@ssw0rd2025!"   # PowerShell
//   export SQL_ADMIN_PASSWORD="Dev@P@ssw0rd2025!"    # bash
using '../main.bicep'

param environmentName  = 'dev'
param location         = 'eastus'
param sqlAdminLogin    = 'sqladmin'
// Falls back to the default value when SQL_ADMIN_PASSWORD is not set — dev only
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', 'Dev@P@ssw0rd2025!')
