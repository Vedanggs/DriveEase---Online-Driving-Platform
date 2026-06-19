// ─────────────────────────────────────────────────────────────────────────────
// Module: network.bicep
// Provisions: VNet + subnets for App Service VNet integration and private
//             endpoints (SQL Server, Service Bus).
//
// Day 27 security pass: isolates the data tier from the public internet.
//   app-subnet  10.0.1.0/24 — App Service VNet integration (delegation)
//   data-subnet 10.0.2.0/24 — Private endpoints (SQL, Service Bus)
// ─────────────────────────────────────────────────────────────────────────────

@description('VNet resource name')
param vnetName string

@description('Azure region')
param location string

resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: 'app-subnet'
        properties: {
          addressPrefix: '10.0.1.0/24'
          // Delegation required for App Service regional VNet integration
          delegations: [
            {
              name: 'appServiceDelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'data-subnet'
        properties: {
          addressPrefix: '10.0.2.0/24'
          // Must be Disabled to allow private endpoint NIC placement in this subnet
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output vnetId      string = vnet.id
output appSubnetId string = vnet.properties.subnets[0].id
output dataSubnetId string = vnet.properties.subnets[1].id
