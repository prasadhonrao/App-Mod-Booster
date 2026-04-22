// app-service.bicep
// Deploys App Service Plan + Web App + User-Assigned Managed Identity in UKSOUTH

param location string = 'uksouth'
param appName string = toLower('app-expensemgmt-${uniqueString(resourceGroup().id)}')
param managedIdentityName string = 'mid-AppModAssist-${uniqueString(resourceGroup().id)}'

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

// App Service Plan - Standard S1
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: toLower('asp-expensemgmt-${uniqueString(resourceGroup().id)}')
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
}

// App Service (Web App)
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
        {
          name: 'ManagedIdentityClientId'
          value: managedIdentity.properties.clientId
        }
      ]
    }
    httpsOnly: true
  }
}

output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output managedIdentityId string = managedIdentity.id
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
output managedIdentityName string = managedIdentity.name
