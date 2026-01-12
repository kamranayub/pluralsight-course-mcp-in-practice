@description('Name for the Azure OpenAI/AI Services account')
param name string

@description('Location for the AI Services account')
param location string = resourceGroup().location

param tags object = {}

@description('SKU for the AI Services account')
@allowed(['S0', 'S1', 'S2', 'S3'])
param sku string = 'S0'

@description('Kind of cognitive services account')
@allowed(['AIServices', 'OpenAI', 'CognitiveServices'])
param kind string = 'AIServices'

@description('Public network access setting')
@allowed(['Enabled', 'Disabled'])
param publicNetworkAccess string = 'Enabled'

@description('Custom subdomain name for the account')
param customSubDomainName string = name

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  kind: kind
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: customSubDomainName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: publicNetworkAccess
    apiProperties: {}
  }
}

output name string = aiServices.name
output id string = aiServices.id
output endpoint string = aiServices.properties.endpoint
output principalId string = aiServices.identity.principalId
