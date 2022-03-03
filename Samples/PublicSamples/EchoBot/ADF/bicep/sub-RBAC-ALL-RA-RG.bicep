param roleDefinitionId string
param principalId string
param principalType string
param name string
param description string // leave these for loggin in the portal
param roledescription string

resource RA 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
    name: name
    properties: {
        roleDefinitionId: roleDefinitionId
        principalType: principalType
        principalId: principalId
    }
}
