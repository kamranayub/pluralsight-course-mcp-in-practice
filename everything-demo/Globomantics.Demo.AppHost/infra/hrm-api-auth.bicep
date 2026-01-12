@description('The name of the Function App to configure')
param name string

@description('The client ID of the HRM API Azure AD application for authentication')
param clientId string

@description('The client ID of the MCP Server Azure AD application for authentication')
param mcpClientId string

var issuerUrl = 'https://login.microsoftonline.com/${tenant().tenantId}/v2.0'
var tokenAudiences array = [clientId, 'api://${clientId}', 'api://${clientId}/user_impersonation']

@description('The allowed AAD client application IDs for the Function App')
var clientApps array = [clientId, mcpClientId]

// Create base application settings
var allAppSettings = {
  WEBSITE_AUTH_AAD_ALLOWED_TENANTS: tenant().tenantId
}

// App settings
resource webAppSettings 'Microsoft.Web/sites/config@2025-03-01' = {
  name: '${name}/appsettings'
  properties: allAppSettings
}

// App settings
resource webSettings 'Microsoft.Web/sites/config@2025-03-01' = {
  name: '${name}/web'
  properties: {
    alwaysOn: false
    cors: {
      allowedOrigins: [
        'https://portal.azure.com'
      ]
      supportCredentials: false
    }
  }
}

resource authSettingsV2 'Microsoft.Web/sites/config@2025-03-01' = {
  name: '${name}/authsettingsV2'
  properties: {
    globalValidation: {
      requireAuthentication: true
      redirectToProvider: 'azureActiveDirectory'
      unauthenticatedClientAction: 'Return302'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: clientId
          openIdIssuer: issuerUrl
          clientSecretSettingName: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
        }
        validation: {
          allowedAudiences: tokenAudiences     
          defaultAuthorizationPolicy: {
            allowedApplications: clientApps
          }       
        }
      }
    }
    login: {
      tokenStore: {
        azureBlobStorage: {}
        enabled: true
        fileSystem: {}
        tokenRefreshExtensionHours: 72
      }
    }
  }
}
