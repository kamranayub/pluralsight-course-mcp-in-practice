param hrmDocumentStorageAccountName string
param searchServiceName string
param managedIdentityPrincipalId string // Principal ID for the User Identity

var storageRoleReaderDefinitionId  = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1' // Storage Blob Data Reader role
var searchIndexDataReaderRoleDefinitionId = '1407120a-92aa-4202-b7e9-c0e197c71c8f' // Search Index Data Reader role ID

resource psDemoHrmStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: hrmDocumentStorageAccountName
}

resource psDemoSearchService 'Microsoft.Search/searchServices@2025-05-01' existing = {
  name: searchServiceName
}

// Role assignment for PS HRM Storage Account (Blob) - User Identity
resource hrmStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(psDemoHrmStorageAccount.id, managedIdentityPrincipalId, storageRoleReaderDefinitionId)
  scope: psDemoHrmStorageAccount
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageRoleReaderDefinitionId)
    principalId: managedIdentityPrincipalId // Use user identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}

// Role assignment for PS Demo Search Service - User Identity
resource psDemoSearchRoleAssignment_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(psDemoSearchService.id, managedIdentityPrincipalId, searchIndexDataReaderRoleDefinitionId)
  scope: psDemoSearchService
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReaderRoleDefinitionId)
    principalId: managedIdentityPrincipalId // Use managed identity ID
    principalType: 'ServicePrincipal' // Managed Identity is a Service Principal
  }
}
