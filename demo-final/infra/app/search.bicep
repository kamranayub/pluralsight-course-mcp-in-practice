@description('Name for the Azure AI Search service')
param name string

@description('Location for the search service')
param location string = resourceGroup().location

param tags object = {}

@description('SKU for the search service')
@allowed(['free', 'basic', 'standard', 'standard2', 'standard3', 'storage_optimized_l1', 'storage_optimized_l2'])
param sku string = 'free'

@description('Number of replicas')
@minValue(1)
@maxValue(12)
param replicaCount int = 1

@description('Number of partitions')
@minValue(1)
@maxValue(12)
param partitionCount int = 1

@description('Hosting mode for the search service')
@allowed(['default', 'highDensity'])
param hostingMode string = 'default'

@description('Public network access setting')
@allowed(['Enabled', 'Disabled'])
param publicNetworkAccess string = 'Enabled'

@description('Disable local authentication (API keys)')
param disableLocalAuth bool = false

@description('Semantic search tier')
@allowed(['disabled', 'free', 'standard'])
param semanticSearch string = 'disabled'

resource searchService 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode
    publicNetworkAccess: publicNetworkAccess
    networkRuleSet: {
      ipRules: []
      bypass: 'None'
    }
    encryptionWithCmk: {
      enforcement: 'Unspecified'
    }
    disableLocalAuth: disableLocalAuth
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
    semanticSearch: semanticSearch
  }
}

output name string = searchService.name
output id string = searchService.id
output endpoint string = 'https://${searchService.name}.search.windows.net'
