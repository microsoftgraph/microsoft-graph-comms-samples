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
        Returns the current mount state of the VHD or ISO file.

    .PARAMETER ImagePath
        Specifies the path of the VHD or ISO file.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $ImagePath
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingMountedImageMessage `
                -f $ImagePath)
        ) -join '' )

    $diskImage = Get-DiskImage -ImagePath $ImagePath

    if ($diskImage.Attached)
    {
        $returnValue = @{
            ImagePath   = $ImagePath
            DriveLetter = ''
            StorageType = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.DiskImage.StorageType] $diskImage.StorageType
            Access      = 'ReadOnly'
            Ensure      = 'Present'
        }

        # Determine the Disk Image Access mode
        if ($diskImage.StorageType `
            -eq [Microsoft.PowerShell.Cmdletization.GeneratedTypes.DiskImage.StorageType]::ISO)
        {
            # Get the Drive Letter the ISO is mounted as
            $volume = $diskImage | Get-Volume
            $returnValue.Driveletter = $volume.DriveLetter
        }
        else
        {
            # Look up the disk and find out if it is readwrite.
            $disk = Get-Disk -Number $diskImage.Number
            if (-not $disk.IsReadOnly)
            {
                $returnValue.Access = 'ReadWrite'
            } # if

            # Find the first 'Basic' partition
            $partitions = $disk | Get-Partition
            $partition = $partitions | Where-Object -Property Type -EQ 'Basic'

            # Find the first volume in the partition and get the mounted Drive Letter
            $volumes = $partition | Get-Volume
            $volume = $volumes | Select-Object -First 1

            $returnValue.Driveletter = $volume.DriveLetter
        } # if
    }
    else
    {
        $returnValue = @{
            ImagePath   = $ImagePath
            Ensure      = 'Absent'
        }
    } # if

    $returnValue
} # Get-TargetResource

<#
    .SYNOPSIS
        Mounts or dismounts the ISO or VHD.

    .PARAMETER ImagePath
        Specifies the path of the VHD or ISO file.

    .PARAMETER DriveLetter
        Specifies the drive letter to mount this VHD or ISO to.

    .PARAMETER StorageType
        Specifies the storage type of a file. If the StorageType parameter is not specified, then the storage type is determined by file extension.

    .PARAMETER Access
        Allows a VHD file to be mounted in read-only or read-write mode. ISO files are mounted in read-only mode regardless of what parameter value you provide.

    .PARAMETER Ensure
        Determines whether the setting should be applied or removed.
#>
function Set-TargetResource
{
    # Should process is called in a helper functions but not directly in Set-TargetResource
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSShouldProcess', '')]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $ImagePath,

        [Parameter()]
        [System.String]
        $DriveLetter,

        [Parameter()]
        [ValidateSet('ISO','VHD','VHDx','VHDSet')]
        [System.String]
        $StorageType,

        [Parameter()]
        [ValidateSet('ReadOnly','ReadWrite')]
        [System.String]
        $Access,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingMountedImageMessage `
                -f $ImagePath)
        ) -join '' )

    # Check the parameter combo passed is valid, and throw if not.
    $null = Test-ParameterValid @PSBoundParameters

    # Get the current mount state of this disk image
    $currentState = Get-TargetResource -ImagePath $ImagePath

    # Remove Ensure from PSBoundParameters so it can be splatted
    $null = $PSBoundParameters.Remove('Ensure')

    if ($Ensure -eq 'Present')
    {
        # Get the normalized DriveLetter (colon removed)
        $normalizedDriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

        # The Disk Image should be mounted
        $needsMount = $false
        if ($currentState.Ensure -eq 'Absent')
        {
            $needsMount = $true
        }
        else
        {
            if ($currentState.DriveLetter -ne $normalizedDriveLetter)
            {
                # The disk image is mounted to the wrong DriveLetter -remount disk
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.DismountingImageMessage `
                            -f $ImagePath,$currentState.DriveLetter)
                    ) -join '' )

                Dismount-DiskImage -ImagePath $ImagePath
                $needsMount = $true
            } # if
        } # if

        if ($currentState.StorageType -ne 'ISO')
        {
            if ($PSBoundParameters.ContainsKey('Access'))
            {
                # For VHD/VHDx/VHDSet disks check the access mode
                if ($currentState.Access -ne $Access)
                {
                    # The access mode is wrong -remount disk
                    Write-Verbose -Message ( @(
                            "$($MyInvocation.MyCommand): "
                            $($script:localizedData.DismountingImageMessage `
                                -f $ImagePath,$currentState.DriveLetter)
                        ) -join '' )

                    Dismount-DiskImage -ImagePath $ImagePath
                    $needsMount = $true
                } # if
            } # if
        } # if

        if ($needsMount)
        {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.MountingImageMessage `
                    -f $ImagePath,$normalizedDriveLetter)
            ) -join '' )

            Mount-DiskImageToLetter @PSBoundParameters
        } # if
    }
    else
    {
        # The Disk Image should not be mounted
        if ($currentState.Ensure -eq 'Present')
        {
            # It is mounted so dismount it
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DismountingImageMessage `
                        -f $ImagePath,$currentState.DriveLetter)
                ) -join '' )

            Dismount-DiskImage -ImagePath $ImagePath
        }
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests if the ISO or VHD file mount is in the correct state.

    .PARAMETER ImagePath
        Specifies the path of the VHD or ISO file.

    .PARAMETER DriveLetter
        Specifies the drive letter to mount this VHD or ISO to.

    .PARAMETER StorageType
        Specifies the storage type of a file. If the StorageType parameter is not
        specified, then the storage type is determined by file extension.

    .PARAMETER Access
        Allows a VHD file to be mounted in read-only or read-write mode. ISO files
        are mounted in read-only mode regardless of what parameter value you provide.

    .PARAMETER Ensure
        Determines whether the setting should be applied or removed.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $ImagePath,

        [Parameter()]
        [System.String]
        $DriveLetter,

        [Parameter()]
        [ValidateSet('ISO','VHD','VHDx','VHDSet')]
        [System.String]
        $StorageType,

        [Parameter()]
        [ValidateSet('ReadOnly','ReadWrite')]
        [System.String]
        $Access,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingMountedImageMessage `
                -f $DriveLetter)
        ) -join '' )

    # Check the parameter combo passed is valid, and throw if not.
    $null = Test-ParameterValid @PSBoundParameters

    # Get the current mount state of this disk image
    $currentState = Get-TargetResource -ImagePath $ImagePath

    if ($Ensure -eq 'Present')
    {
        # Get the normalized DriveLetter (colon removed)
        $normalizedDriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

        # The Disk Image should be mounted
        if ($currentState.Ensure -eq 'Absent')
        {
            # The disk image isn't mounted
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.ImageIsNotMountedButShouldBeMessage `
                        -f $ImagePath,$normalizedDriveLetter)
                ) -join '' )
            return $false
        } # if

        if ($currentState.DriveLetter -ne $normalizedDriveLetter)
        {
            # The disk image is mounted to the wrong DriveLetter
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.ImageIsMountedToTheWrongDriveLetterMessage `
                        -f $ImagePath,$currentState.DriveLetter,$normalizedDriveLetter)
                ) -join '' )
            return $false
        } # if

        if ($currentState.StorageType -ne 'ISO')
        {
            if ($PSBoundParameters.ContainsKey('Access'))
            {
                # For VHD/VHDx/VHDSet disks check the access mode
                if ($currentState.Access -ne $Access)
                {
                    Write-Verbose -Message ( @(
                            "$($MyInvocation.MyCommand): "
                            $($script:localizedData.VHDAccessModeMismatchMessage `
                                -f $ImagePath,$normalizedDriveLetter,$currentState.Access,$Access)
                        ) -join '' )
                    return $false
                } # if
            } # if
        } # if

        # The Disk Image is mounted to the expected Drive - nothing to change.
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.ImageIsMountedAndShouldBeMessage `
                    -f $ImagePath,$normalizedDriveLetter)
            ) -join '' )
    }
    else
    {
        # The Disk Image should not be mounted
        if ($currentState.Ensure -eq 'Present')
        {
            # The disk image is mounted
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.ImageIsMountedButShouldNotBeMessage `
                        -f $ImagePath,$currentState.DriveLetter)
                ) -join '' )
            return $false
        } # if

        # The image is not mounted and should not be
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.ImageIsNotMountedAndShouldNotBeMessage `
                    -f $ImagePath)
            ) -join '' )
    } # if

    # No changes are needed
    return $true
} # Test-TargetResource

<#
    .SYNOPSIS
        Validates that the parameters passed are valid. If the parameter combination
        is invalid then an exception will be thrown. Also validates the DriveLetter
        value that is passed is valid.

    .PARAMETER ImagePath
        Specifies the path of the VHD or ISO file.

    .PARAMETER DriveLetter
        Specifies the drive letter to mount this VHD or ISO to.

    .PARAMETER StorageType
        Specifies the storage type of a file. If the StorageType parameter is not
        specified, then the storage type is determined by file extension.

    .PARAMETER Access
        Allows a VHD file to be mounted in read-only or read-write mode. ISO files
        are mounted in read-only mode regardless of what parameter value you provide.

    .PARAMETER Ensure
        Determines whether the setting should be applied or removed.

    .OUTPUTS
        If ensure is present then returns a normalized array of Drive Letters.
#>
function Test-ParameterValid
{
    [CmdletBinding()]
    [OutputType([String[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $ImagePath,

        [Parameter()]
        [System.String]
        $DriveLetter,

        [Parameter()]
        [ValidateSet('ISO','VHD','VHDx','VHDSet')]
        [System.String]
        $StorageType,

        [Parameter()]
        [ValidateSet('ReadOnly','ReadWrite')]
        [System.String]
        $Access,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    if ($Ensure -eq 'Absent')
    {
        if ($PSBoundParameters.ContainsKey('DriveLetter'))
        {
            # The DriveLetter should not be set if Ensure is Absent
            New-InvalidOperationException `
                -Message ($script:localizedData.InvalidParameterSpecifiedError -f `
                    'Absent','DriveLetter')
        } # if

        if ($PSBoundParameters.ContainsKey('StorageType'))
        {
            # The StorageType should not be set if Ensure is Absent
            New-InvalidOperationException `
                -Message ($script:localizedData.InvalidParameterSpecifiedError -f `
                    'Absent','StorageType')
        } # if

        if ($PSBoundParameters.ContainsKey('Access'))
        {
            # The Access should not be set if Ensure is Absent
            New-InvalidOperationException `
                -Message ($script:localizedData.InvalidParameterSpecifiedError -f `
                    'Absent','Access')
        } # if
    }
    else
    {
        if (-not (Test-Path -Path $ImagePath))
        {
            # The file specified by ImagePath should be found
            New-InvalidOperationException `
                -Message ($script:localizedData.DiskImageFileNotFoundError -f `
                    $ImagePath)
        } # if

        if ($PSBoundParameters.ContainsKey('DriveLetter'))
        {
            # Test the Drive Letter to ensure it is valid
            $normalizedDriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter
        }
        else
        {
            # Drive letter is not specified but Ensure is present.
            New-InvalidOperationException `
                -Message ($script:localizedData.InvalidParameterNotSpecifiedError -f `
                    'Present','DriveLetter')
        } # if
    } # if
} # Test-ParameterValid

<#
    .SYNOPSIS
        Mounts a Disk Image to a specific Drive Letter.

    .PARAMETER ImagePath
        Specifies the path of the VHD or ISO file.

    .PARAMETER DriveLetter
        Specifies the drive letter to mount this VHD or ISO to.

    .PARAMETER StorageType
        Specifies the storage type of a file. If the StorageType parameter is not
        specified, then the storage type is determined by file extension.

    .PARAMETER Access
        Allows a VHD file to be mounted in read-only or read-write mode. ISO files
        are mounted in read-only mode regardless of what parameter value you provide.
#>
function Mount-DiskImageToLetter
{
    [CmdletBinding()]
    [OutputType([String[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $ImagePath,

        [Parameter()]
        [System.String]
        $DriveLetter,

        [Parameter()]
        [ValidateSet('ISO','VHD','VHDx','VHDSet')]
        [System.String]
        $StorageType,

        [Parameter()]
        [ValidateSet('ReadOnly','ReadWrite')]
        [System.String]
        $Access
    )

    # Get the normalized DriveLetter (colon removed)
    $normalizedDriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    # Mount the Diskimage
    $mountParams = @{
        ImagePath = $ImagePath
    }

    if ($PSBoundParameters.ContainsKey('Access'))
    {
        $mountParams += @{
            Access = $Access
        }
    }  # if
    $null = Mount-DiskImage @mountParams

    # Get the DiskImage object
    $diskImage = Get-DiskImage -ImagePath $ImagePath

    # Determine the Storage Type expected
    if (-not $PSBoundParameters.ContainsKey('StorageType'))
    {
        $StorageType = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.DiskImage.StorageType] $diskImage.StorageType
    } # if

    # Different StorageType images require different methods of getting the Volume object.
    if ($StorageType -eq [Microsoft.PowerShell.Cmdletization.GeneratedTypes.DiskImage.StorageType]::ISO)
    {
        # This is a ISO diskimage
        $volume = $diskImage | Get-Volume
    }
    else
    {
        # This is a VHD/VHDx/VHDSet diskimage
        $disk = Get-Disk -Number $diskImage.Number

        # Find the first 'Basic' partition to mount
        $partitions = $disk | Get-Partition
        $partition = $partitions | Where-Object -Property Type -EQ 'Basic'

        # Find the first volume in the partition and get the mounted Drive Letter
        $volumes = $partition | Get-Volume
        $volume = $volumes | Select-Object -First 1
    } # if

    $currentDriveLetter = $volume.DriveLetter

    # Verify that the drive letter assigned to the drive is the one needed.
    if ($currentDriveLetter -ne $normalizedDriveLetter)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.ChangingImageDriveLetterMessage `
                    -f $ImagePath,$currentDriveLetter,$normalizedDriveLetter)
            ) -join '' )

        <#
            Use CIM to change the DriveLetter.
            The Win32_Volume must be looked up using the ObjectId found in the Volume object
            ObjectId is more verbose than DeviceId in Windows 10 Anniversary Edition, look for
            DeviceId in the ObjectId string to match volumes.
        #>
        $cimVolume = Get-CimInstance -ClassName Win32_Volume |
            Where-Object -FilterScript {
                $volume.ObjectId.IndexOf($_.DeviceId) -ne -1
            }

        Set-CimInstance `
            -InputObject $cimVolume `
            -Property @{
                DriveLetter = "$($normalizedDriveLetter):"
            }
    } # if
} # Mount-DiskImageToLetter

Export-ModuleMember -Function *-TargetResource
