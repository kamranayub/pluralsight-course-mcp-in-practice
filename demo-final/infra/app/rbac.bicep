@description('Name of the function storage account')
param functionStorageAccountName string

@description('Name of the document storage account')
param documentStorageAccountName string

@description('Name of the search service')
param searchServiceName string

@description('Name of the AI services account')
param aiServicesAccountName string

@description('Name of Application Insights')
param appInsightsName string

@description('Principal ID for the Managed Identity')
param managedIdentityPrincipalId string

@description('Principal ID for the User Identity (optional, for testing/debugging)')
param userIdentityPrincipalId string = ''

@description('Flag to enable user identity role assignments')
param allowUserIdentityPrincipal bool = false

// Define Role Definition IDs
var storageRoleDefinitionId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner
var searchIndexDataContributorRoleId = '8ebe5a00-799e-43f5-93ac-243d3dce84a7' // Search Index Data Contributor
var searchServiceContributorRoleId = '7ca78c08-252a-4471-8644-bb5ff32d4ba0' // Search Service Contributor
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908' // Cognitive Services User
var monitoringRoleDefinitionId = '3913510d-42f4-4e42-8a64-420c390055eb' // Monitoring Metrics Publisher

// Reference existing resources
resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: functionStorageAccountName
}

resource documentStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: documentStorageAccountName
}

resource searchService 'Microsoft.Search/searchServices@2024-06-01-preview' existing = {
  name: searchServiceName
}

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: aiServicesAccountName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// Role assignments for Managed Identity

// Function Storage - Blob Data Owner (for function runtime)
resource functionStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionStorageAccount.id, managedIdentityPrincipalId, storageRoleDefinitionId)
  scope: functionStorageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Document Storage - Blob Data Owner (for document access)
resource documentStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(documentStorageAccount.id, managedIdentityPrincipalId, storageRoleDefinitionId)
  scope: documentStorageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Search Service - Search Index Data Contributor
resource searchIndexRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, searchIndexDataContributorRoleId)
  scope: searchService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Search Service - Search Service Contributor
resource searchServiceRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, searchServiceContributorRoleId)
  scope: searchService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', searchServiceContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// AI Services - Cognitive Services User
resource aiServicesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, managedIdentityPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Application Insights - Monitoring Metrics Publisher
resource appInsightsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(applicationInsights.id, managedIdentityPrincipalId, monitoringRoleDefinitionId)
  scope: applicationInsights
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', monitoringRoleDefinitionId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignments for User Identity (for testing/debugging)

// Function Storage - User Identity
resource functionStorageRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(functionStorageAccount.id, userIdentityPrincipalId, storageRoleDefinitionId)
  scope: functionStorageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}

// Document Storage - User Identity
resource documentStorageRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(documentStorageAccount.id, userIdentityPrincipalId, storageRoleDefinitionId)
  scope: documentStorageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleDefinitionId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}

// Search Service - User Identity (Index Data Contributor)
resource searchIndexRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(searchService.id, userIdentityPrincipalId, searchIndexDataContributorRoleId)
  scope: searchService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataContributorRoleId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}

// AI Services - User Identity
resource aiServicesRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(aiServices.id, userIdentityPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}

// Application Insights - User Identity
resource appInsightsRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(applicationInsights.id, userIdentityPrincipalId, monitoringRoleDefinitionId)
  scope: applicationInsights
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', monitoringRoleDefinitionId)
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}
