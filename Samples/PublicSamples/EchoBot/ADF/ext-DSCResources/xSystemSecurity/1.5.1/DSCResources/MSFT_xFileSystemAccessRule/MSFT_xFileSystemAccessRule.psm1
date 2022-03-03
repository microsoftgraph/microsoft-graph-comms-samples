<#
    .SYNOPSIS
        Gets the rights of the specified filesystem object for the specified identity.
    .PARAMETER Path
        The path to the item that should have permissions set.
    .PARAMETER Identity
        The identity to set permissions for.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Path,

        [Parameter(Mandatory = $true)]
        [String]
        $Identity
    )

    $result = @{
        Ensure = 'Absent'
        Path = $Path
        Identity = $Identity
        Rights = [System.string[]] @()
        IsActiveNode = $true
    }

    if ( -not ( Test-Path -Path $Path ) )
    {
        $isClusterResource = $false

        # Is the node a member of a WSFC?
        $msCluster = Get-CimInstance -Namespace root/MSCluster -ClassName MSCluster_Cluster -ErrorAction SilentlyContinue

        if ( $msCluster )
        {
            Write-Verbose -Message "$($env:COMPUTERNAME) is a member of the Windows Server Failover Cluster '$($msCluster.Name)'"

            # Is the defined path built off of a known mount point in the cluster?
            $clusterPartition = Get-CimInstance -Namespace root/MSCluster -ClassName MSCluster_ClusterDiskPartition |
                Where-Object -FilterScript {
                    $currentPartition = $_

                    $currentPartition.MountPoints | ForEach-Object -Process {
                        [regex]::Escape($Path) -match "^$($_)"
                    }
                }

            # Get the possible owner nodes for the partition
            [array]$possibleOwners = $clusterPartition |
                Get-CimAssociatedInstance -ResultClassName 'MSCluster_Resource' |
                    Get-CimAssociatedInstance -Association 'MSCluster_ResourceToPossibleOwner' |
                        Select-Object -ExpandProperty Name -Unique

            # Ensure the current node is a possible owner of the drive
            if ( $possibleOwners -contains $env:COMPUTERNAME )
            {
                $isClusterResource = $true
                $result.IsActiveNode = $false
                $result.Ensure = 'Present'
            }
            else
            {
                Write-Verbose -Message "'$($env:COMPUTERNAME)' is not a possible owner for '$Path'."
            }
        }

        if ( -not $isClusterResource )
        {
            throw "Unable to get ACL for '$Path' because it does not exist"
        }
    }
    else
    {
        $acl = Get-Acl -Path $Path
        $accessRules = $acl.Access

        # Set works without BUILTIN\, but Get fails (silently) without this logic.
        # This is tested by the 'Users' group in the Functional
        # Integration test logic, which is actually BUILTIN\USERS per ACLs, however
        # this is not obvious to users and results in unexpected functionality
        # such as successful SETs, but TEST's that fail every time, so this regex
        # workaround for the common windows identifier prefixes makes behavior consistent.
        # Local groups are fully qualified with $env:ComputerName\.
        $regexEscapedIdentity = [RegEx]::Escape($Identity)
        $escapedComputerName = [RegEx]::Escape($ENV:ComputerName)
        $regex = "^(NT AUTHORITY|BUILTIN|NT SERVICES|$escapedComputerName)\\$regexEscapedIdentity"
        $matchingRules = $accessRules | Where-Object -FilterScript { $_.IdentityReference -eq $Identity -or $_.IdentityReference -match $regex }
        if ( $matchingRules )
        {
            $result.Ensure = 'Present'
            $result.Rights = @(
                ( $matchingRules.FileSystemRights -split ', ' ) | Select-Object -Unique
            )
        }
    }

    return $result
}

<#
    .SYNOPSIS
        Sets the rights of the specified filesystem object for the specified identity.
    .PARAMETER Path
        The path to the item that should have permissions set.
    .PARAMETER Identity
        The identity to set permissions for.

    .PARAMETER Rights
        The permissions to include in this rule. Optional if Ensure is set to value 'Absent'.
    .PARAMETER Ensure
        Present to create the rule, Absent to remove an existing rule. Default value is 'Present'.
    .PARAMETER ProcessOnlyOnActiveNode
        Specifies that the resource will only determine if a change is needed if the target node is the active host of the filesystem object. The user the configuration is run as must have permission to the Windows Server Failover Cluster.
        Not used in Set-TargetResource.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Path,

        [Parameter(Mandatory = $true)]
        [String]
        $Identity,

        [Parameter()]
        [ValidateSet(
            'ListDirectory',
            'ReadData',
            'WriteData',
            'CreateFiles',
            'CreateDirectories',
            'AppendData',
            'ReadExtendedAttributes',
            'WriteExtendedAttributes',
            'Traverse',
            'ExecuteFile',
            'DeleteSubdirectoriesAndFiles',
            'ReadAttributes',
            'WriteAttributes',
            'Write',
            'Delete',
            'ReadPermissions',
            'Read',
            'ReadAndExecute',
            'Modify',
            'ChangePermissions',
            'TakeOwnership',
            'Synchronize',
            'FullControl'
        )]
        [String[]]
        $Rights,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter()]
        [Boolean]
        $ProcessOnlyOnActiveNode
    )

    if ( -not ( Test-Path -Path $Path ) )
    {
        throw ( "The path '$Path' does not exist." )
    }

    $acl = Get-ACLAccess -Path $Path
    $accessRules = $acl.Access

    if ( $Ensure -eq 'Present' )
    {
        # Validate the rights parameter was passed
        if ( -not $PSBoundParameters.ContainsKey('Rights') )
        {
            throw "No rights were specified for '$Identity' on '$Path'"
        }

        Write-Verbose -Message "Setting access rules for '$Identity' on '$Path'"

        $newFileSystemAccessRuleParameters = @{
            TypeName = 'System.Security.AccessControl.FileSystemAccessRule'
            ArgumentList = @(
                $Identity,
                [System.Security.AccessControl.FileSystemRights]$Rights,
                'ContainerInherit,ObjectInherit',
                'None',
                'Allow'
            )
        }

        $ar = New-Object @newFileSystemAccessRuleParameters
        $acl.SetAccessRule($ar)

        Set-Acl -Path $Path -AclObject $acl
    }

    if ($Ensure -eq 'Absent')
    {
        $identityRule = $accessRules | Where-Object -FilterScript {
            $_.IdentityReference -eq $Identity
        } | Select-Object -First 1

        if ( $null -ne $identityRule )
        {
            Write-Verbose -Message "Removing access rules for '$Identity' on '$Path'"
            $acl.RemoveAccessRule($identityRule) | Out-Null
            Set-Acl -Path $Path -AclObject $acl
        }
    }
}

<#
    .SYNOPSIS
        Tests the rights of the specified filesystem object for the specified identity.
    .PARAMETER Path
        The path to the item that should have permissions set.
    .PARAMETER Identity
        The identity to set permissions for.

    .PARAMETER Rights
        The permissions to include in this rule. Optional if Ensure is set to value 'Absent'.
    .PARAMETER Ensure
        Present to create the rule, Absent to remove an existing rule. Default value is 'Present'.
    .PARAMETER ProcessOnlyOnActiveNode
        Specifies that the resource will only determine if a change is needed if the target node is the active host of the filesystem object. The user the configuration is run as must have permission to the Windows Server Failover Cluster.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Path,

        [Parameter(Mandatory = $true)]
        [String]
        $Identity,

        [Parameter()]
        [ValidateSet(
            'ListDirectory',
            'ReadData',
            'WriteData',
            'CreateFiles',
            'CreateDirectories',
            'AppendData',
            'ReadExtendedAttributes',
            'WriteExtendedAttributes',
            'Traverse',
            'ExecuteFile',
            'DeleteSubdirectoriesAndFiles',
            'ReadAttributes',
            'WriteAttributes',
            'Write',
            'Delete',
            'ReadPermissions',
            'Read',
            'ReadAndExecute',
            'Modify',
            'ChangePermissions',
            'TakeOwnership',
            'Synchronize',
            'FullControl'
        )]
        [String[]]
        $Rights,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter()]
        [Boolean]
        $ProcessOnlyOnActiveNode
    )

    $result = $true

    $getTargetResourceParameters = @{
        Path = $Path
        Identity = $Identity
    }

    $currentValues = Get-TargetResource @getTargetResourceParameters

    <#
        If this is supposed to process on the active node, and this is not the
        active node, don't bother evaluating the test.
    #>
    if ( $ProcessOnlyOnActiveNode -and -not $currentValues.IsActiveNode )
    {
        Write-Verbose -Message ( 'The node "{0}" is not actively hosting the path "{1}". Exiting the test.' -f $env:COMPUTERNAME,$Path )
        return $result
    }


    switch ( $Ensure )
    {
        'Absent'
        {
            # If no rights were passed
            if ( -not $PSBoundParameters.ContainsKey('Rights') )
            {
                # Set rights to an empty array
                $Rights = @()
            }
            if ( $currentValues.Rights -and (-not $Rights) )
            {
                $result = $false
                Write-Verbose -Message ( 'Returning false. The identity "{0}" has the rights "{1}" when expected no rights by the Ensure Absent.' -f $Identity,( $currentValues.Rights -join ', ' ) )
            }
            elseif ( -not $currentValues.Rights )
            {
                $result = $true
                Write-Verbose -Message ( 'Returning true. The identity "{0}" has no rights as expected by the Ensure Absent.' -f $Identity)
            }
            elseif ( $Rights ) # always hit, but just clarifying what the actual case is by filling in the if block
            {
                foreach ($right in $Rights)
                {
                    $notAllowed = [System.Security.AccessControl.FileSystemRights]$right

                    # If any rights that we want to deny are individually a full subset of existing rights...
                    $currentRightResult = -not ($notAllowed -eq ( $notAllowed -band ([System.Security.AccessControl.FileSystemRights] $currentValues.Rights ) ) )

                    if (-not $currentRightResult)
                    {
                        Write-Verbose -Message ( 'Testing right {0} absence: false. The identity "{1}" has the rights "{2}" which include "{0}", which are included in the desired Absent rights "{3}".' -f $notAllowed, $Identity,( $currentValues.Rights -join ', ' ), ($Rights -join ', ') )
                    }
                    else
                    {
                        Write-Verbose -Message ( 'Testing right {0} absence: true. The identity "{1}" has the rights "{2}" which do not contain "{0}".' -f $notAllowed, $Identity,( $currentValues.Rights -join ', ' ) )
                    }

                    $result = $result -and $currentRightResult
                }
                Write-Verbose -Message ( 'Returning {0}.' -f $result )
            }
        }

        'Present'
        {
            # Validate the rights parameter was passed
            if ( -not $PSBoundParameters.ContainsKey('Rights') )
            {
                throw "No rights were specified for '$Identity' on '$Path'"
            }
            # This isn't always the same as the input if parts of the input are subset permissions, so pre-cast it.
            # For example [System.Security.AccessControl.FileSystemRights]@('Modify', 'Read', 'Write') is actually just 'Modify' within the flagged enum, so test as such to avoid false test failures.
            $expected = [System.Security.AccessControl.FileSystemRights]$Rights

            $result = $false
            if ($currentValues.Rights)
            {
                # At minimum the AND result of the current and expected rights should be the expected rights (allow extra rights, but not missing).
                # Otherwise permission flags are missing from the enum.
                $result = $expected -eq ($expected -band ([System.Security.AccessControl.FileSystemRights] $currentValues.Rights))
                Write-Verbose -Message ( 'Returning {0}. The identity "{1}" has the rights "{2}". The expected rights are "{3}" (combined from input Rights "{4}").' -f  $result, $Identity,( $currentValues.Rights -join ', ' ), $expected,( $Rights -join ', ' ) )
            }
        }
    }

    return $result
}

<#
    .SYNOPSIS
        Retrieves the access control list from a filesystem object.
    .PARAMETER Path
        The path of the filesystem object to retrieve the ACL from.
#>
function Get-AclAccess
{
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Path
    )

    return (Get-Item -Path $Path).GetAccessControl('Access')
}
