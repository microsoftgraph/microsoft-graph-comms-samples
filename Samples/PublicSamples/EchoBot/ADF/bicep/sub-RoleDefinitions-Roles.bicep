param RoleName string
param description string
param notActions array
param actions array
param assignableScopes array

targetScope = 'subscription'

resource roleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' = {
    name: guid(subscription().id,RoleName)
    properties: {
        roleName: RoleName
        description: description
        permissions: [
            {
                actions: actions
                notActions: notActions
            }
        ]
        assignableScopes: contains(assignableScopes,null) ? array(subscription().id) : assignableScopes
    }
}
