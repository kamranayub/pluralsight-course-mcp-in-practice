@description('Name for the function app storage account')
param functionStorageName string

@description('Name for the document storage account (HRM documents)')
param documentStorageName string

@description('Location for all resources')
param location string = resourceGroup().location

param tags object = {}

@description('Container name for document storage')
param documentContainerName string = 'globomanticshrm'

@description('Container name for deployment package storage')
param deploymentStorageContainerName string

// Function App Storage Account (for function runtime and deployment)
module functionStorage 'br/public:avm/res/storage/storage-account:0.8.3' = {
  name: 'functionStorage'
  params: {
    name: functionStorageName
    location: location
    tags: tags
    kind: 'Storage'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // Disable local authentication methods as per policy
    dnsEndpointType: 'Standard'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    blobServices: {
      containers: [
        {
          name: deploymentStorageContainerName
        }
        {
          name: 'azure-webjobs-hosts'
        }
        {
          name: 'azure-webjobs-secrets'
        }
      ]
      deleteRetentionPolicyEnabled: false
    }
    minimumTlsVersion: 'TLS1_2'
  }
}

// Document Storage Account (for HRM policy documents and search index)
module documentStorage 'br/public:avm/res/storage/storage-account:0.8.3' = {
  name: 'documentStorage'
  params: {
    name: documentStorageName
    location: location
    tags: tags
    kind: 'StorageV2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Required for some scenarios like search indexer
    dnsEndpointType: 'Standard'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    blobServices: {
      containers: [
        {
          name: documentContainerName
        }
      ]
      containerDeleteRetentionPolicyEnabled: true
      containerDeleteRetentionPolicyDays: 7
      deleteRetentionPolicyEnabled: true
      deleteRetentionPolicyDays: 7
    }
    largeFileSharesState: 'Enabled'
    minimumTlsVersion: 'TLS1_2'
    accessTier: 'Hot'
  }
}

output functionStorageName string = functionStorage.outputs.name
output documentStorageName string = documentStorage.outputs.name
output functionStorageId string = functionStorage.outputs.resourceId
output documentStorageId string = documentStorage.outputs.resourceId
