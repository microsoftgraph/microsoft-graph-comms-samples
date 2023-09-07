$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Storage Common Module.
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'StorageDsc.Common' `
            -ChildPath 'StorageDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings.
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

<#
    .SYNOPSIS
        Returns the current state of the wait for drive resource.

    .PARAMETER DriveLetter
        Specifies the name of the drive to wait for.

    .PARAMETER RetryIntervalSec
        Specifies the number of seconds to wait for the drive to become available.

    .PARAMETER RetryCount
        The number of times to loop the retry interval while waiting for the drive.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $DriveLetter,

        [Parameter()]
        [UInt32]
        $RetryIntervalSec = 10,

        [Parameter()]
        [UInt32]
        $RetryCount = 60
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingWaitForVolumeStatusMessage -f $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    $returnValue = @{
        DriveLetter      = $DriveLetter
        RetryIntervalSec = $RetryIntervalSec
        RetryCount       = $RetryCount
    }
    return $returnValue
} # function Get-TargetResource

<#
    .SYNOPSIS
        Sets the current state of the wait for drive resource.

    .PARAMETER DriveLetter
        Specifies the name of the drive to wait for.

    .PARAMETER RetryIntervalSec
        Specifies the number of seconds to wait for the drive to become available.

    .PARAMETER RetryCount
        The number of times to loop the retry interval while waiting for the drive.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $DriveLetter,

        [Parameter()]
        [UInt32]
        $RetryIntervalSec = 10,

        [Parameter()]
        [UInt32]
        $RetryCount = 60
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingForVolumeStatusMessage -f $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    $volumeFound = $false

    for ($count = 0; $count -lt $RetryCount; $count++)
    {
        $volume = Get-Volume -DriveLetter $DriveLetter -ErrorAction SilentlyContinue
        if ($volume)
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.VolumeFoundMessage -f $DriveLetter)
                ) -join '' )

            $volumeFound = $true
            break
        }
        else
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.VolumeNotFoundRetryingMessage -f $DriveLetter,$RetryIntervalSec)
                ) -join '' )

            Start-Sleep -Seconds $RetryIntervalSec

            <#
                This command forces a refresh of the PS Drive subsystem.
                So triggers any "missing" drives to show up.
            #>
            $null = Get-PSDrive
        } # if
    } # for

    if (-not $volumeFound)
    {
        New-InvalidOperationException `
            -Message $($script:localizedData.VolumeNotFoundAfterError -f $DriveLetter,$RetryCount)
    } # if
} # function Set-TargetResource

<#
    .SYNOPSIS
        Tests the current state of the wait for drive resource.

    .PARAMETER DriveLetter
        Specifies the name of the drive to wait for.

    .PARAMETER RetryIntervalSec
        Specifies the number of seconds to wait for the drive to become available.

    .PARAMETER RetryCount
        The number of times to loop the retry interval while waiting for the drive.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $DriveLetter,

        [Parameter()]
        [UInt32]
        $RetryIntervalSec = 10,

        [Parameter()]
        [UInt32]
        $RetryCount = 60
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingForVolumeStatusMessage -f $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    <#
        This command forces a refresh of the PS Drive subsystem.
        So triggers any "missing" drives to show up.
    #>
    $null = Get-PSDrive

    $volume = Get-Volume -DriveLetter $DriveLetter -ErrorAction SilentlyContinue

    if ($volume)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.VolumeFoundMessage -f $DriveLetter)
            ) -join '' )

        return $true
    }

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.VolumeNotFoundMessage -f $DriveLetter)
        ) -join '' )

    return $false
} # function Test-TargetResource

Export-ModuleMember -Function *-TargetResource
