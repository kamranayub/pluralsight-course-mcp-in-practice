targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
@allowed([
  'australiaeast'
  'eastus'
  'eastus2'
  'westus'
  'westus2'
  'westus3'
  'centralus'
  'northcentralus'
  'southcentralus'
  'westcentralus'
  'canadacentral'
  'brazilsouth'
  'northeurope'
  'westeurope'
  'uksouth'
  'ukwest'
  'francecentral'
  'germanywestcentral'
  'norwayeast'
  'swedencentral'
  'switzerlandnorth'
  'uaenorth'
  'southafricanorth'
  'eastasia'
  'southeastasia'
  'japaneast'
  'japanwest'
  'koreacentral'
  'centralindia'
  'southindia'
])
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@description('Whether to deploy a new AI services. If false, you must provide an existing AI Search Service endpoint to the MCP server.')
param deployAiServices bool

@description('The client ID of the Globomantics HRM Azure AD application for authentication')
param aadHrmClientId string

@description('The client ID of the MCP Server Azure AD application for authentication')
param aadMcpClientId string

// Optional parameter overrides
param hrmApiServiceName string = ''
param hrmApiUserAssignedIdentityName string = ''
param applicationInsightsName string = ''
param appServicePlanName string = ''
param logAnalyticsName string = ''
param resourceGroupName string = ''
param functionStorageAccountName string = ''
param documentStorageAccountName string = ''
param searchServiceName string = ''
param aiServicesAccountName string = ''

@description('Id of the user identity to be used for testing and debugging. This is not required in production. Leave empty if not needed.')
param principalId string = ''

// Load abbreviations for resource naming
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Generate resource names
var hrmApiFunctionAppName = !empty(hrmApiServiceName) ? hrmApiServiceName : '${abbrs.webSitesFunctions}globomantics-hrm-${resourceToken}'
var deploymentStorageContainerName = 'app-package-${take(hrmApiFunctionAppName, 32)}-${take(toLower(uniqueString(hrmApiFunctionAppName, resourceToken)), 7)}'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// User assigned managed identity to be used by the function app
module hrmApiUserAssignedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.1' = {
  name: 'hrmApiUserAssignedIdentity'
  scope: rg
  params: {
    location: location
    tags: tags
    name: !empty(hrmApiUserAssignedIdentityName) ? hrmApiUserAssignedIdentityName : '${abbrs.managedIdentityUserAssignedIdentities}hrm-api-${resourceToken}'
  }
}

// Create an App Service Plan for the HRM API Function App
module appServicePlan 'br/public:avm/res/web/serverfarm:0.1.1' = {
  name: 'appserviceplan'
  scope: rg
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}${resourceToken}'
    sku: {
      name: 'FC1'
      tier: 'FlexConsumption'
    }
    reserved: true
    location: location
    tags: tags
  }
}

// Storage Accounts (Function storage + Document storage)
module storage './app/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    functionStorageName: !empty(functionStorageAccountName) ? functionStorageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
    documentStorageName: !empty(documentStorageAccountName) ? documentStorageAccountName : '${abbrs.storageStorageAccounts}hrmdocs${resourceToken}'
    location: location
    tags: tags
    documentContainerName: 'globomanticshrm'
    deploymentStorageContainerName: deploymentStorageContainerName
  }
}

// Azure AI Search Service
module search './app/search.bicep' = if(deployAiServices) {
  name: 'search'
  scope: rg
  params: {
    name: !empty(searchServiceName) ? searchServiceName : '${abbrs.searchSearchServices}${resourceToken}'
    location: 'centralus' // Free tier only available in certain regions
    tags: tags
    sku: 'free'
    disableLocalAuth: false
    semanticSearch: 'disabled'
  }
}

// Azure OpenAI / AI Services
module aiServices './app/aiservices.bicep' = if (deployAiServices) {
  name: 'aiServices'
  scope: rg
  params: {
    name: !empty(aiServicesAccountName) ? aiServicesAccountName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    sku: 'S0'
    kind: 'AIServices'
    customSubDomainName: !empty(aiServicesAccountName) ? aiServicesAccountName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
  }
}

// Monitor application with Azure Monitor - Log Analytics and Application Insights
module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.7.0' = {
  name: '${uniqueString(deployment().name, location)}-loganalytics'
  scope: rg
  params: {
    name: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    location: location
    tags: tags
    dataRetention: 30
  }
}

module monitoring 'br/public:avm/res/insights/component:0.4.1' = {
  name: '${uniqueString(deployment().name, location)}-appinsights'
  scope: rg
  params: {
    name: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    location: location
    tags: tags
    workspaceResourceId: logAnalytics.outputs.resourceId
    disableLocalAuth: true
  }
}

// HRM API Function App
module hrmApi './app/api.bicep' = {
  name: 'hrmApi'
  scope: rg
  params: {
    name: hrmApiFunctionAppName
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.name
    appServicePlanId: appServicePlan.outputs.resourceId
    runtimeName: 'dotnet-isolated'
    runtimeVersion: '8.0'
    storageAccountName: storage.outputs.functionStorageName
    deploymentStorageContainerName: deploymentStorageContainerName
    identityId: hrmApiUserAssignedIdentity.outputs.resourceId
    identityClientId: hrmApiUserAssignedIdentity.outputs.clientId
    serviceName: 'hrm-api'
    instanceMemoryMB: 512
    maximumInstanceCount: 100
    clientId: aadHrmClientId
    issuerUrl: 'https://login.microsoftonline.com/${tenant().tenantId}/v2.0'
    clientApps: [
      aadHrmClientId
      aadMcpClientId
    ]
    tokenAudiences: [
      'api://${aadHrmClientId}'
      'api://${aadHrmClientId}/user_impersonation'
      aadHrmClientId
    ]
    appSettings: {
      AZURE_TENANT_ID: tenant().tenantId
      AZURE_CLIENT_ID: hrmApiUserAssignedIdentity.outputs.clientId
      MICROSOFT_PROVIDER_AUTHENTICATION_SECRET: 'copy-from-hrm-api-entra-app-registration'
    }
  }
}

// Consolidated Role Assignments
module rbac './app/rbac.bicep' = {
  name: 'rbacAssignments'
  scope: rg
  params: {
    enableAiServices: deployAiServices
    functionStorageAccountName: storage.outputs.functionStorageName
    documentStorageAccountName: storage.outputs.documentStorageName
    searchServiceName: search.?outputs.name ?? ''
    aiServicesAccountName: aiServices.?outputs.name ?? ''
    appInsightsName: monitoring.outputs.name
    managedIdentityPrincipalId: hrmApiUserAssignedIdentity.outputs.principalId
    userIdentityPrincipalId: principalId
    allowUserIdentityPrincipal: !empty(principalId)
  }
}

// Outputs
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.connectionString
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_HRM_API_NAME string = hrmApi.outputs.SERVICE_API_NAME
output SERVICE_HRM_API_URI string = hrmApi.outputs.SERVICE_API_URI
output AZURE_FUNCTION_NAME string = hrmApi.outputs.SERVICE_API_NAME
output AZURE_SEARCH_ENDPOINT string = search.?outputs.endpoint ?? ''
output AZURE_OPENAI_ENDPOINT string = aiServices.?outputs.endpoint ?? ''
