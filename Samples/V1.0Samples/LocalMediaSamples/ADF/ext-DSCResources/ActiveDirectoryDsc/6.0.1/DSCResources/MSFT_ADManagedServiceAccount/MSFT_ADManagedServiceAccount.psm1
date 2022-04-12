$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'

$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'ActiveDirectoryDsc.Common'
Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'ActiveDirectoryDsc.Common.psm1')

$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_ADManagedServiceAccount'

$script:errorCodeKdsRootKeyNotFound = -2146893811

<#
    .SYNOPSIS
        Returns the current state of an Active Directory managed service account.

    .PARAMETER ServiceAccountName
    Specifies the Security Account Manager (SAM) account name of the managed service account (ldapDisplayName
    'sAMAccountName'). To be compatible with older operating systems, create a SAM account name that is 20 characters
    or less. Once created, the user's SamAccountName and CN cannot be changed.

    .PARAMETER AccountType
        The type of managed service account. Standalone will create a Standalone Managed Service Account (sMSA) and
        Group will create a Group Managed Service Account (gMSA).

    .PARAMETER Credential
        Specifies the user account credentials to use to perform this task.
        This is only required if not executing the task on a domain controller or using the DomainController parameter.

    .PARAMETER DomainController
        Specifies the Active Directory Domain Controller instance to use to perform the task.
        This is only required if not executing the task on a domain controller.

    .PARAMETER MembershipAttribute
        Active Directory attribute used to perform membership operations for Group Managed Service Accounts (gMSAs).
        If not specified, this value defaults to SamAccountName. Only used when 'Group' is selected for 'AccountType'.

    .NOTES
        Used Functions:
            Name                          | Module
            ------------------------------|--------------------------
            Get-ADObject                  | ActiveDirectory
            Get-ADServiceAccount          | ActiveDirectory
            Assert-Module                 | ActiveDirectoryDsc.Common
            Get-ADCommonParameters        | ActiveDirectoryDsc.Common
            Get-ADObjectParentDN          | ActiveDirectoryDsc.Common
            New-InvalidOperationException | ActiveDirectoryDsc.Common
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ServiceAccountName,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Group', 'Standalone')]
        [System.String]
        $AccountType,

        [Parameter()]
        [ValidateNotNull()]
        [System.Management.Automation.PSCredential]
        [System.Management.Automation.CredentialAttribute()]
        $Credential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DomainController,

        [Parameter()]
        [ValidateSet('SamAccountName', 'DistinguishedName', 'ObjectSid', 'ObjectGUID')]
        [System.String]
        $MembershipAttribute = 'SamAccountName'
    )

    Assert-Module -ModuleName 'ActiveDirectory'
    $adServiceAccountParameters = Get-ADCommonParameters @PSBoundParameters

    Write-Verbose -Message ($script:localizedData.RetrievingManagedServiceAccountMessage -f
        $ServiceAccountName)

    try
    {
        $adServiceAccount = Get-ADServiceAccount @adServiceAccountParameters -Properties @(
            'DistinguishedName'
            'Description'
            'DisplayName'
            'ObjectClass'
            'Enabled'
            'PrincipalsAllowedToRetrieveManagedPassword'
            'KerberosEncryptionType'
        )
    }

    catch [Microsoft.ActiveDirectory.Management.ADIdentityNotFoundException]
    {
        Write-Verbose -Message ($script:localizedData.ManagedServiceAccountNotFoundMessage -f
            $AccountType, $ServiceAccountName)
        $adServiceAccount = $null
    }
    catch
    {
        $errorMessage = $script:localizedData.RetrievingManagedServiceAccountError -f $ServiceAccountName
        New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
    }

    if ($adServiceAccount)
    {
        # Resource exists
        $managedPasswordPrincipals = @()

        if ($adServiceAccount.ObjectClass -eq 'msDS-ManagedServiceAccount')
        {
            $existingAccountType = 'Standalone'
        }
        else
        {
            $existingAccountType = 'Group'

            Write-Verbose -Message ($script:localizedData.RetrievingManagedPasswordPrincipalsMessage -f
                $MembershipAttribute)

            foreach ($identity in $adServiceAccount.PrincipalsAllowedToRetrieveManagedPassword)
            {
                try
                {
                    $principal = (Get-ADObject -Identity $identity -Properties $MembershipAttribute).$MembershipAttribute
                }
                catch [Microsoft.ActiveDirectory.Management.ADIdentityNotFoundException]
                {
                    # Add unresolved SID as principal if the identity could not be found
                    $principal = $identity
                }
                catch
                {
                    $errorMessage = $script:localizedData.RetrievingManagedPasswordPrincipalsError -f $identity
                    New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
                }

                $managedPasswordPrincipals += $principal
            }
        }

        $targetResource = @{
            ServiceAccountName        = $ServiceAccountName
            AccountType               = $existingAccountType
            Path                      = Get-ADObjectParentDN -DN $adServiceAccount.DistinguishedName
            Description               = $adServiceAccount.Description
            DisplayName               = $adServiceAccount.DisplayName
            DistinguishedName         = $adServiceAccount.DistinguishedName
            Enabled                   = $adServiceAccount.Enabled
            KerberosEncryptionType    = $adServiceAccount.KerberosEncryptionType -split (', ')
            ManagedPasswordPrincipals = $managedPasswordPrincipals
            MembershipAttribute       = $MembershipAttribute
            Ensure                    = 'Present'
        }
    }
    else
    {
        # Resource does not exist
        $targetResource = @{
            ServiceAccountName        = $ServiceAccountName
            AccountType               = $AccountType
            Path                      = $null
            Description               = $null
            DisplayName               = $null
            DistinguishedName         = $null
            Enabled                   = $false
            KerberosEncryptionType    = @()
            ManagedPasswordPrincipals = @()
            MembershipAttribute       = $MembershipAttribute
            Ensure                    = 'Absent'
        }
    }

    return $targetResource
} #end function Get-TargetResource

<#
    .SYNOPSIS
        Tests if an Active Directory managed service account is in the desired state.

    .PARAMETER ServiceAccountName
        Specifies the Security Account Manager (SAM) account name of the managed service account (ldapDisplayName
        'sAMAccountName'). To be compatible with older operating systems, create a SAM account name that is 20
        characters or less. Once created, the user's SamAccountName and CN cannot be changed.

    .PARAMETER AccountType
        The type of managed service account. Standalone will create a Standalone Managed Service Account (sMSA) and
        Group will create a Group Managed Service Account (gMSA).

    .PARAMETER Credential
        Specifies the user account credentials to use to perform this task.
        This is only required if not executing the task on a domain controller or using the DomainController parameter.

    .PARAMETER Description
        Specifies the description of the account (ldapDisplayName 'description').

    .PARAMETER DisplayName
        Specifies the display name of the account (ldapDisplayName 'displayName').

    .PARAMETER DomainController
        Specifies the Active Directory Domain Controller instance to use to perform the task.
        This is only required if not executing the task on a domain controller.

    .PARAMETER Ensure
        Specifies whether the user account is created or deleted. If not specified, this value defaults to Present.

    .PARAMETER KerberosEncryptionType
        Specifies which Kerberos encryption types the account supports when creating service tickets.
        This value sets the encryption types supported flags of the Active Directory msDS-SupportedEncryptionTypes
        attribute.

    .PARAMETER ManagedPasswordPrincipals
        Specifies the membership policy for systems which can use a group managed service account. (ldapDisplayName
        'msDS-GroupMSAMembership'). Only used when 'Group' is selected for 'AccountType'.

    .PARAMETER MembershipAttribute
        Active Directory attribute used to perform membership operations for Group Managed Service Accounts (gMSAs).
        If not specified, this value defaults to SamAccountName. Only used when 'Group' is selected for 'AccountType'.

    .PARAMETER Path
        Specifies the X.500 path of the Organizational Unit (OU) or container where the new account is created.
        Specified as a Distinguished Name (DN).

    .NOTES
        Used Functions:
            Name                          | Module
            ------------------------------|--------------------------
            Compare-ResourcePropertyState | ActiveDirectoryDsc.Common
#>
function Test-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', "",
        Justification = 'False positive on ManagedPasswordPrincipals')]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ServiceAccountName,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Group', 'Standalone')]
        [System.String]
        $AccountType,

        [Parameter()]
        [ValidateNotNull()]
        [System.Management.Automation.PSCredential]
        [System.Management.Automation.CredentialAttribute()]
        $Credential,

        [Parameter()]
        [System.String]
        $Description,

        [Parameter()]
        [System.String]
        $DisplayName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DomainController,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('None', 'RC4', 'AES128', 'AES256')]
        [System.String[]]
        $KerberosEncryptionType,

        [Parameter()]
        [System.String[]]
        $ManagedPasswordPrincipals,

        [Parameter()]
        [ValidateSet('SamAccountName', 'DistinguishedName', 'ObjectSid', 'ObjectGUID')]
        [System.String]
        $MembershipAttribute = 'SamAccountName',

        [Parameter()]
        [System.String]
        $Path
    )

    # Need to set these parameters to compare if users are using the default parameter values
    [HashTable] $parameters = $PSBoundParameters
    $parameters['MembershipAttribute'] = $MembershipAttribute

    $getTargetResourceParameters = @{
        ServiceAccountName  = $ServiceAccountName
        AccountType         = $AccountType
        DomainController    = $DomainController
        MembershipAttribute = $MembershipAttribute
    }

    @($getTargetResourceParameters.Keys) |
        ForEach-Object {
            if (-not $parameters.ContainsKey($_))
            {
                $getTargetResourceParameters.Remove($_)
            }
        }

    $getTargetResourceResult = Get-TargetResource @getTargetResourceParameters

    if ($getTargetResourceResult.Ensure -eq 'Present')
    {
        # Resource exists
        if ($Ensure -eq 'Present')
        {
            # Resource should exist
            $propertiesNotInDesiredState = (
                Compare-ResourcePropertyState -CurrentValues $getTargetResourceResult -DesiredValues $parameters `
                    -IgnoreProperties 'DomainController', 'Credential' | Where-Object -Property InDesiredState -eq $false)

            if ($propertiesNotInDesiredState)
            {
                $inDesiredState = $false
            }
            else
            {
                # Resource is in desired state
                Write-Verbose -Message ($script:localizedData.ManagedServiceAccountInDesiredStateMessage -f
                    $AccountType, $ServiceAccountName)
                $inDesiredState = $true
            }
        }
        else
        {
            # Resource should not exist
            Write-Verbose -Message ($script:localizedData.ResourceExistsButShouldNotMessage -f
                $AccountType, $ServiceAccountName)
            $inDesiredState = $false
        }
    }
    else
    {
        # Resource does not exist
        if ($Ensure -eq 'Present')
        {
            # Resource should exist
            Write-Verbose -Message ($script:localizedData.ResourceDoesNotExistButShouldMessage -f
                $AccountType, $ServiceAccountName)
            $inDesiredState = $false
        }
        else
        {
            # Resource should not exist
            Write-Verbose -Message ($script:localizedData.ManagedServiceAccountInDesiredStateMessage -f
                $AccountType, $ServiceAccountName)
            $inDesiredState = $true
        }
    }

    $inDesiredState
} #end function Test-TargetResource

<#
    .SYNOPSIS
        Sets the state of an Active Directory managed service account.

    .PARAMETER ServiceAccountName
        Specifies the Security Account Manager (SAM) account name of the managed service account (ldapDisplayName
        'sAMAccountName'). To be compatible with older operating systems, create a SAM account name that is 20
        characters or less. Once created, the user's SamAccountName and CN cannot be changed.

    .PARAMETER AccountType
        The type of managed service account. Standalone will create a Standalone Managed Service Account (sMSA) and
        Group will create a Group Managed Service Account (gMSA).

    .PARAMETER Credential
        Specifies the user account credentials to use to perform this task.
        This is only required if not executing the task on a domain controller or using the DomainController parameter.

    .PARAMETER Description
        Specifies the description of the account (ldapDisplayName 'description').

    .PARAMETER DisplayName
        Specifies the display name of the account (ldapDisplayName 'displayName').

    .PARAMETER DomainController
        Specifies the Active Directory Domain Controller instance to use to perform the task.
        This is only required if not executing the task on a domain controller.

    .PARAMETER Ensure
        Specifies whether the user account is created or deleted. If not specified, this value defaults to Present.

    .PARAMETER KerberosEncryptionType
        Specifies which Kerberos encryption types the account supports when creating service tickets.
        This value sets the encryption types supported flags of the Active Directory msDS-SupportedEncryptionTypes
        attribute.

    .PARAMETER ManagedPasswordPrincipals
        Specifies the membership policy for systems which can use a group managed service account. (ldapDisplayName
        'msDS-GroupMSAMembership'). Only used when 'Group' is selected for 'AccountType'.

    .PARAMETER MembershipAttribute
        Active Directory attribute used to perform membership operations for Group Managed Service Accounts (gMSAs).
        If not specified, this value defaults to SamAccountName. Only used when 'Group' is selected for 'AccountType'.

    .PARAMETER Path
        Specifies the X.500 path of the Organizational Unit (OU) or container where the new account is created.
        Specified as a Distinguished Name (DN).

    .NOTES
        Used Functions:
            Name                          | Module
            ------------------------------|--------------------------
            Get-ADDomain                  | ActiveDirectory
            Move-ADObject                 | ActiveDirectory
            New-ADServiceAccount          | ActiveDirectory
            Remove-ADServiceAccount       | ActiveDirectory
            Set-ADServiceAccount          | ActiveDirectory
            Compare-ResourcePropertyState | ActiveDirectoryDsc.Common
            Get-ADCommonParameters        | ActiveDirectoryDsc.Common
            Get-DomainName                | ActiveDirectoryDsc.Common
            New-InvalidOperationException | ActiveDirectoryDsc.Common
#>

function Set-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', "",
        Justification = 'False positive on ManagedPasswordPrincipals')]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ServiceAccountName,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Group', 'Standalone')]
        [System.String]
        $AccountType,

        [Parameter()]
        [ValidateNotNull()]
        [System.Management.Automation.PSCredential]
        [System.Management.Automation.CredentialAttribute()]
        $Credential,

        [Parameter()]
        [System.String]
        $Description,

        [Parameter()]
        [System.String]
        $DisplayName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DomainController,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('None', 'RC4', 'AES128', 'AES256')]
        [System.String[]]
        $KerberosEncryptionType,

        [Parameter()]
        [System.String[]]
        $ManagedPasswordPrincipals,

        [Parameter()]
        [ValidateSet('SamAccountName', 'DistinguishedName', 'ObjectSid', 'ObjectGUID')]
        [System.String]
        $MembershipAttribute = 'SamAccountName',

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $Path
    )

    # Need to set these to compare if not specified since user is using defaults
    [HashTable] $parameters = $PSBoundParameters
    $parameters['MembershipAttribute'] = $MembershipAttribute

    $adServiceAccountParameters = Get-ADCommonParameters @parameters

    $getTargetResourceParameters = @{
        ServiceAccountName  = $ServiceAccountName
        AccountType         = $AccountType
        DomainController    = $DomainController
        MembershipAttribute = $MembershipAttribute
    }

    @($getTargetResourceParameters.Keys) |
        ForEach-Object {
            if (-not $parameters.ContainsKey($_))
            {
                $getTargetResourceParameters.Remove($_)
            }
        }

    $getTargetResourceResult = Get-TargetResource @getTargetResourceParameters

    if ($Ensure -eq 'Present')
    {
        # Resource should be present
        if ($getTargetResourceResult.Ensure -eq 'Present')
        {
            # Resource is present
            $createNewAdServiceAccount = $false
            $propertiesNotInDesiredState = (
                Compare-ResourcePropertyState -CurrentValues $getTargetResourceResult -DesiredValues $parameters `
                    -IgnoreProperties 'DomainController', 'Credential' | Where-Object -Property InDesiredState -eq $false)
            if ($propertiesNotInDesiredState)
            {
                if ($propertiesNotInDesiredState.ParameterName -contains 'AccountType')
                {
                    # AccountType has changed, so the account needs recreating
                    Write-Verbose -Message ($script:localizedData.RecreatingManagedServiceAccountMessage -f
                        $AccountType, $ServiceAccountName)
                    try
                    {
                        Remove-ADServiceAccount @adServiceAccountParameters -Confirm:$false
                    }
                    catch
                    {
                        $errorMessage = ($script:localizedData.RemovingManagedServiceAccountError -f
                            $AccountType, $ServiceAccountName)
                        New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
                    }

                    $createNewAdServiceAccount = $true
                }
                else
                {
                    $setServiceAccountParameters = $adServiceAccountParameters.Clone()
                    $setAdServiceAccountRequired = $false
                    $moveAdServiceAccountRequired = $false

                    foreach ($property in $propertiesNotInDesiredState)
                    {
                        if ($property.ParameterName -eq 'Path')
                        {
                            # The path has changed, so the account needs moving, but not until after any other changes
                            $moveAdServiceAccountRequired = $true
                        }
                        else
                        {
                            $setAdServiceAccountRequired = $true

                            Write-Verbose -Message ($script:localizedData.UpdatingManagedServiceAccountPropertyMessage -f
                                $AccountType, $ServiceAccountName, $property.ParameterName, ($property.Expected -join ', '))

                            if ($property.ParameterName -eq 'ManagedPasswordPrincipals' -and $AccountType -eq 'Group')
                            {
                                $setServiceAccountParameters.Add('PrincipalsAllowedToRetrieveManagedPassword',
                                    $ManagedPasswordPrincipals)
                            }
                            else
                            {
                                $SetServiceAccountParameters.Add($property.ParameterName, $property.Expected)
                            }
                        }
                    }

                    if ($setAdServiceAccountRequired)
                    {
                        try
                        {
                            Set-ADServiceAccount @setServiceAccountParameters
                        }
                        catch
                        {
                            $errorMessage = ($script:localizedData.SettingManagedServiceAccountError -f
                                $AccountType, $ServiceAccountName)
                            New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
                        }
                    }

                    if ($moveAdServiceAccountRequired)
                    {
                        Write-Verbose -Message ($script:localizedData.MovingManagedServiceAccountMessage -f
                            $AccountType, $ServiceAccountName, $getTargetResourceResult.Path, $Path)
                        $moveADObjectParameters = $adServiceAccountParameters.Clone()
                        $moveADObjectParameters.Identity = $getTargetResourceResult.DistinguishedName
                        try
                        {
                            Move-ADObject @moveADObjectParameters -TargetPath $Path
                        }
                        catch
                        {
                            $errorMessage = ($script:localizedData.MovingManagedServiceAccountError -f
                                $AccountType, $ServiceAccountName, $getTargetResourceResult.Path, $Path)
                            New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
                        }
                    }
                }
            }
        }
        else
        {
            # Resource is absent
            $createNewAdServiceAccount = $true
        }

        if ($createNewAdServiceAccount)
        {
            if (-not $parameters.ContainsKey('Path'))
            {
                # Get default MSA path as one has not been specified
                try
                {
                    $domainDistinguishedName = (Get-ADDomain).DistinguishedName
                }
                catch
                {
                    $errorMessage = $script:localizedData.GettingADDomainError
                    New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
                }

                $messagePath = "CN=Managed Service Accounts,$domainDistinguishedName"
            }
            else
            {
                $messagePath = $Path
            }

            Write-Verbose -Message ($script:localizedData.AddingManagedServiceAccountMessage -f
                $AccountType, $ServiceAccountName, $messagePath)

            $newAdServiceAccountParameters = Get-ADCommonParameters @parameters -UseNameParameter

            if ($parameters.ContainsKey('Description'))
            {
                $newAdServiceAccountParameters.Description = $Description
            }

            if ($parameters.ContainsKey('DisplayName'))
            {
                $newAdServiceAccountParameters.DisplayName = $DisplayName
            }

            if ($parameters.ContainsKey('Path'))
            {
                $newAdServiceAccountParameters.Path = $Path
            }

            if ( $AccountType -eq 'Standalone' )
            {
                # Create standalone managed service account
                $newAdServiceAccountParameters.RestrictToSingleComputer = $true
            }
            else
            {
                # Create group managed service account
                $newAdServiceAccountParameters.DNSHostName = "$ServiceAccountName.$(Get-DomainName)"

                if ($parameters.ContainsKey('ManagedPasswordPrincipals'))
                {
                    $newAdServiceAccountParameters.PrincipalsAllowedToRetrieveManagedPassword = `
                        $ManagedPasswordPrincipals
                }
            }

            try
            {
                New-ADServiceAccount @newAdServiceAccountParameters
            }
            catch [Microsoft.ActiveDirectory.Management.ADException]
            {
                if ($_.Exception.ErrorCode -eq $script:errorCodeKdsRootKeyNotFound)
                {
                    $errorMessage = ($script:localizedData.KdsRootKeyNotFoundError -f
                        $ServiceAccountName)
                }
                else
                {
                    $errorMessage = ($script:localizedData.AddingManagedServiceAccountError -f
                        $AccountType, $ServiceAccountName, $messagePath)
                }

                New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
            }
            catch
            {
                $errorMessage = ($script:localizedData.AddingManagedServiceAccountError -f
                    $AccountType, $ServiceAccountName, $messagePath)
                New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
            }
        }
    }
    else
    {
        # Resource should be absent
        if ($getTargetResourceResult.Ensure -eq 'Present')
        {
            # Resource is present
            Write-Verbose -Message ($script:localizedData.RemovingManagedServiceAccountMessage -f
                $AccountType, $ServiceAccountName)

            try
            {
                Remove-ADServiceAccount @adServiceAccountParameters -Confirm:$false
            }
            catch
            {
                $errorMessage = ($script:localizedData.RemovingManagedServiceAccountError -f
                    $AccountType, $ServiceAccountName)
                New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
            }
        }
        else
        {
            # Resource is absent
            Write-Verbose -Message ($script:localizedData.ManagedServiceAccountInDesiredStateMessage -f
                $AccountType, $ServiceAccountName)
        }
    }
} #end function Set-TargetResource

Export-ModuleMember -Function *-TargetResource
