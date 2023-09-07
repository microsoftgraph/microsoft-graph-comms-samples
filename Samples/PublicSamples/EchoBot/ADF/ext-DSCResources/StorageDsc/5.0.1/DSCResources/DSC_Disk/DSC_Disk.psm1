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
        Returns the current state of the Disk and Partition.

    .PARAMETER DriveLetter
        Specifies the preferred letter to assign to the disk volume.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER PartitionStyle
        Specifies the partition style of the disk. Defaults to GPT.
        This parameter is not used in Get-TargetResource.

    .PARAMETER Size
        Specifies the size of new volume (use all available space on disk if not provided).
        This parameter is not used in Get-TargetResource.

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.
        This parameter is not used in Get-TargetResource.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.
        This parameter is not used in Get-TargetResource.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.
        This parameter is not used in Get-TargetResource.

    .PARAMETER AllowDestructive
        Specifies if potentially destructive operations may occur.
        This parameter is not used in Get-TargetResource.

    .PARAMETER ClearDisk
        Specifies if the disks partition schema should be removed entirely, even if data and OEM
        partitions are present. Only possible with AllowDestructive enabled.
        This parameter is not used in Get-TargetResource.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $DriveLetter,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

        [Parameter()]
        [ValidateSet('GPT', 'MBR')]
        [System.String]
        $PartitionStyle = 'GPT',

        [Parameter()]
        [System.UInt64]
        $Size,

        [Parameter()]
        [System.String]
        $FSLabel,

        [Parameter()]
        [System.UInt32]
        $AllocationUnitSize,

        [Parameter()]
        [ValidateSet('NTFS', 'ReFS')]
        [System.String]
        $FSFormat = 'NTFS',

        [Parameter()]
        [System.Boolean]
        $AllowDestructive,

        [Parameter()]
        [System.Boolean]
        $ClearDisk
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingDiskMessage -f $DiskIdType, $DiskId, $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    # Get the Disk using the identifiers supplied
    $disk = Get-DiskByIdentifier `
        -DiskId $DiskId `
        -DiskIdType $DiskIdType

    $partition = Get-Partition `
        -DriveLetter $DriveLetter `
        -ErrorAction SilentlyContinue | Select-Object -First 1

    $volume = Get-Volume `
        -DriveLetter $DriveLetter `
        -ErrorAction SilentlyContinue

    $blockSize = (Get-CimInstance `
            -Query "SELECT BlockSize from Win32_Volume WHERE DriveLetter = '$($DriveLetter):'" `
            -ErrorAction SilentlyContinue).BlockSize

    return @{
        DiskId             = $DiskId
        DiskIdType         = $DiskIdType
        DriveLetter        = $partition.DriveLetter
        PartitionStyle     = $disk.PartitionStyle
        Size               = $partition.Size
        FSLabel            = $volume.FileSystemLabel
        AllocationUnitSize = $blockSize
        FSFormat           = $volume.FileSystem
    }
} # Get-TargetResource

<#
    .SYNOPSIS
        Initializes the Disk and Partition and assigns the drive letter.

    .PARAMETER DriveLetter
        Specifies the preferred letter to assign to the disk volume.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER PartitionStyle
        Specifies the partition style of the disk. Defaults to GPT.

    .PARAMETER Size
        Specifies the size of new volume. Leave empty to use the remaining free space.

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.

    .PARAMETER AllowDestructive
        Specifies if potentially destructive operations may occur.

    .PARAMETER ClearDisk
        Specifies if the disks partition schema should be removed entirely, even if data and OEM
        partitions are present. Only possible with AllowDestructive enabled.
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
        $DriveLetter,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

        [Parameter()]
        [ValidateSet('GPT', 'MBR')]
        [System.String]
        $PartitionStyle = 'GPT',

        [Parameter()]
        [System.UInt64]
        $Size,

        [Parameter()]
        [System.String]
        $FSLabel,

        [Parameter()]
        [System.UInt32]
        $AllocationUnitSize,

        [Parameter()]
        [ValidateSet('NTFS', 'ReFS')]
        [System.String]
        $FSFormat = 'NTFS',

        [Parameter()]
        [System.Boolean]
        $AllowDestructive,

        [Parameter()]
        [System.Boolean]
        $ClearDisk
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingDiskMessage -f $DiskIdType, $DiskId, $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    # Get the Disk using the identifiers supplied
    $disk = Get-DiskByIdentifier `
        -DiskId $DiskId `
        -DiskIdType $DiskIdType

    if ($disk.IsOffline)
    {
        # Disk is offline, so bring it online
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.SetDiskOnlineMessage -f $DiskIdType, $DiskId)
            ) -join '' )

        $disk | Set-Disk -IsOffline $false
    } # if

    if ($disk.IsReadOnly)
    {
        # Disk is read-only, so make it read/write
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.SetDiskReadWriteMessage -f $DiskIdType, $DiskId)
            ) -join '' )

        $disk | Set-Disk -IsReadOnly $false
    } # if

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingDiskPartitionStyleMessage -f $DiskIdType, $DiskId)
        ) -join '' )

    if ($AllowDestructive -and $ClearDisk -and $disk.PartitionStyle -ne 'RAW')
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.ClearingDiskMessage -f $DiskIdType, $DiskId)
            ) -join '' )

        $disk | Clear-Disk -RemoveData -RemoveOEM -Confirm:$true

        # Requery the disk
        $disk = Get-DiskByIdentifier `
            -DiskId $DiskId `
            -DiskIdType $DiskIdType
    }

    if ($disk.PartitionStyle -eq 'RAW')
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.InitializingDiskMessage -f $DiskIdType, $DiskId, $PartitionStyle)
            ) -join '' )

        $disk | Initialize-Disk -PartitionStyle $PartitionStyle
    }
    else
    {
        if ($disk.PartitionStyle -eq $PartitionStyle)
        {
            # The disk partition is already initialized with the correct partition style
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DiskAlreadyInitializedMessage `
                            -f $DiskIdType, $DiskId, $disk.PartitionStyle)
                ) -join '' )

        }
        else
        {
            # This disk is initialized but with the incorrect partition style
            New-InvalidOperationException `
                -Message ($script:localizedData.DiskInitializedWithWrongPartitionStyleError `
                    -f $DiskIdType, $DiskId, $disk.PartitionStyle, $PartitionStyle)
        }
    }

    # Get the partitions on the disk
    $partition = $disk | Get-Partition -ErrorAction SilentlyContinue

    # Check if the disk has an existing partition assigned to the drive letter
    $assignedPartition = $partition |
        Where-Object -Property DriveLetter -eq $DriveLetter

    # Check if existing partition already has file system on it
    if ($null -eq $assignedPartition)
    {
        # There is no partiton with this drive letter
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DriveNotFoundOnPartitionMessage `
                        -f $DiskIdType, $DiskId, $DriveLetter)
            ) -join '' )

        # Are there any partitions defined on this disk?
        if ($partition)
        {
            # There are partitions defined - identify if one matches the size required
            if ($Size)
            {
                # Find the first basic partition matching the size
                $partition = $partition |
                    Where-Object -FilterScript { $_.Type -eq 'Basic' -and $_.Size -eq $Size } |
                    Select-Object -First 1

                if ($partition)
                {
                    # A partition matching the required size was found
                    Write-Verbose -Message ($script:localizedData.MatchingPartitionFoundMessage `
                            -f $DiskIdType, $DiskId, $partition.PartitionNumber)
                }
                else
                {
                    # A partition matching the required size was not found
                    Write-Verbose -Message ($script:localizedData.MatchingPartitionNotFoundMessage `
                            -f $DiskIdType, $DiskId)
                } # if
            }
            else
            {
                <#
                    No size specified, so see if there is a partition that has a volume
                    matching the file system type that is not assigned to a drive letter.
                #>
                Write-Verbose -Message ($script:localizedData.MatchingPartitionNoSizeMessage `
                        -f $DiskIdType, $DiskId)

                $searchPartitions = $partition | Where-Object -FilterScript {
                    $_.Type -eq 'Basic' -and -not [System.Char]::IsLetter($_.DriveLetter)
                }

                $partition = $null

                foreach ($searchPartition in $searchPartitions)
                {
                    # Look for the volume in the partition.
                    Write-Verbose -Message ($script:localizedData.SearchForVolumeMessage `
                            -f $DiskIdType, $DiskId, $searchPartition.PartitionNumber, $FSFormat)

                    $searchVolumes = $searchPartition | Get-Volume

                    $volumeMatch = $searchVolumes | Where-Object -FilterScript {
                        $_.FileSystem -eq $FSFormat
                    }

                    if ($volumeMatch)
                    {
                        <#
                            Found a partition with a volume that matches file system
                            type and not assigned a drive letter.
                        #>
                        $partition = $searchPartition

                        Write-Verbose -Message ($script:localizedData.VolumeFoundMessage `
                                -f $DiskIdType, $DiskId, $searchPartition.PartitionNumber, $FSFormat)

                        break
                    } # if
                } # foreach
            } # if
        } # if

        # Do we need to create a new partition?
        if (-not $partition)
        {
            # Attempt to create a new partition
            $partitionParams = @{
                DriveLetter = $DriveLetter
            }

            if ($Size)
            {
                # Use only a specific size
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.CreatingPartitionMessage `
                                -f $DiskIdType, $DiskId, $DriveLetter, "$($Size/1KB) KB")
                    ) -join '' )

                $partitionParams['Size'] = $Size
            }
            else
            {
                # Use the entire disk
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.CreatingPartitionMessage `
                                -f $DiskIdType, $DiskId, $DriveLetter, 'all free space')
                    ) -join '' )

                $partitionParams['UseMaximumSize'] = $true
            } # if

            # Create the partition.
            $partition = $disk | New-Partition @partitionParams

            <#
                After creating the partition it can take a few seconds for it to become writeable
                Wait for up to 30 seconds for the parition to become writeable
            #>
            $timeAtStart = Get-Date
            $minimumTimeToWait = $timeAtStart + (New-Timespan -Second 3)
            $maximumTimeToWait = $timeAtStart + (New-Timespan -Second 30)

            while (($partitionstate.IsReadOnly -and (Get-Date) -lt $maximumTimeToWait) `
                -or ((Get-Date) -lt $minimumTimeToWait))
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        ($script:localizedData.NewPartitionIsReadOnlyMessage `
                                -f $DiskIdType, $DiskId, $partition.PartitionNumber)
                    ) -join '' )

                Start-Sleep -Seconds 1

                # Pull the partition details again to check if it is readonly
                $partitionstate = $partition | Get-Partition
            } # while
        } # if

        if ($partition.IsReadOnly)
        {
            # The partition is still readonly - throw an exception
            New-InvalidOperationException `
                -Message ($script:localizedData.NewParitionIsReadOnlyError `
                    -f $DiskIdType, $DiskId, $partition.PartitionNumber)
        } # if

        $assignDriveLetter = $true
    }
    else
    {
        # The disk already has a partition on it that is assigned to the Drive Letter
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.PartitionAlreadyAssignedMessage `
                        -f $DriveLetter, $assignedPartition.PartitionNumber)
            ) -join '' )

        $assignDriveLetter = $false

        $supportedSize = $assignedPartition | Get-PartitionSupportedSize

        <#
            If the parition size was not specified then try and make the partition
            use all possible space on the disk.
        #>
        if (-not ($PSBoundParameters.ContainsKey('Size')))
        {
            $Size = $supportedSize.SizeMax
        }

        if ($assignedPartition.Size -ne $Size)
        {
            # A patition resize is required
            if ($AllowDestructive)
            {
                if ($FSFormat -eq 'ReFS')
                {
                    Write-Warning -Message ( @(
                            "$($MyInvocation.MyCommand): "
                            $($script:localizedData.ResizeRefsNotPossibleMessage `
                                    -f $DriveLetter, $assignedPartition.Size, $Size)
                        ) -join '' )

                }
                else
                {
                    Write-Verbose -Message ( @(
                            "$($MyInvocation.MyCommand): "
                            $($script:localizedData.SizeMismatchCorrectionMessage `
                                    -f $DriveLetter, $assignedPartition.Size, $Size)
                        ) -join '' )

                    if ($Size -gt $supportedSize.SizeMax)
                    {
                        New-InvalidArgumentException -Message ( @(
                                "$($MyInvocation.MyCommand): "
                                $($script:localizedData.FreeSpaceViolationError `
                                        -f $DriveLetter, $assignedPartition.Size, $Size, $supportedSize.SizeMax)
                            ) -join '' ) -ArgumentName 'Size' -ErrorAction Stop
                    }

                    $assignedPartition | Resize-Partition -Size $Size
                }
            }
            else
            {
                # A partition resize was required but is not allowed
                Write-Warning -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.ResizeNotAllowedMessage `
                                -f $DriveLetter, $assignedPartition.Size, $Size)
                    ) -join '' )
            }
        }
    }

    # Get the Volume on the partition
    $volume = $partition | Get-Volume

    # Is the volume already formatted?
    if ($volume.FileSystem -eq '')
    {
        # The volume is not formatted
        $formatVolumeParameters = @{
            FileSystem = $FSFormat
            Confirm    = $false
        }

        if ($FSLabel)
        {
            # Set the File System label on the new volume
            $formatVolumeParameters['NewFileSystemLabel'] = $FSLabel
        } # if

        if ($AllocationUnitSize)
        {
            # Set the Allocation Unit Size on the new volume
            $formatVolumeParameters['AllocationUnitSize'] = $AllocationUnitSize
        } # if

        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.FormattingVolumeMessage -f $formatVolumeParameters.FileSystem)
            ) -join '' )

        # Format the volume
        $volume = $partition | Format-Volume @formatVolumeParameters
    }
    else
    {
        # The volume is already formatted
        if ($PSBoundParameters.ContainsKey('FSFormat'))
        {
            # Check the filesystem format
            $fileSystem = $volume.FileSystem
            if ($fileSystem -ne $FSFormat)
            {
                # The file system format does not match
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.FileSystemFormatMismatch `
                                -f $DriveLetter, $fileSystem, $FSFormat)
                    ) -join '' )

                if ($AllowDestructive)
                {
                    Write-Verbose -Message ( @(
                            "$($MyInvocation.MyCommand): "
                            $($script:localizedData.VolumeFormatInProgressMessage `
                                    -f $DriveLetter, $fileSystem, $FSFormat)
                        ) -join '' )

                    $formatParam = @{
                        FileSystem = $FSFormat
                        Force      = $true
                    }

                    if ($PSBoundParameters.ContainsKey('AllocationUnitSize'))
                    {
                        $formatParam.Add('AllocationUnitSize', $AllocationUnitSize)
                    }

                    $Volume | Format-Volume @formatParam
                }
            } # if
        } # if

        # Check the volume label
        if ($PSBoundParameters.ContainsKey('FSLabel'))
        {
            # The volume should have a label assigned
            if ($volume.FileSystemLabel -ne $FSLabel)
            {
                # The volume lable needs to be changed because it is different.
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.ChangingVolumeLabelMessage `
                                -f $DriveLetter, $FSLabel)
                    ) -join '' )

                $volume | Set-Volume -NewFileSystemLabel $FSLabel
            } # if
        } # if
    } # if

    # Assign the Drive Letter if it isn't assigned
    if ($assignDriveLetter -and ($partition.DriveLetter -ne $DriveLetter))
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.AssigningDriveLetterMessage -f $DriveLetter)
            ) -join '' )

        $null = $partition | Set-Partition -NewDriveLetter $DriveLetter

        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.SuccessfullyInitializedMessage -f $DriveLetter)
            ) -join '' )
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests if the disk is initialized, the partion exists and the drive letter is assigned.

    .PARAMETER DriveLetter
        Specifies the preferred letter to assign to the disk volume.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER PartitionStyle
        Specifies the partition style of the disk. Defaults to GPT.

    .PARAMETER Size
        Specifies the size of new volume. Leave empty to use the remaining free space.

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.

    .PARAMETER AllowDestructive
        Specifies if potentially destructive operations may occur.

    .PARAMETER ClearDisk
        Specifies if the disks partition schema should be removed entirely, even if data and OEM
        partitions are present. Only possible with AllowDestructive enabled.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $DriveLetter,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

        [Parameter()]
        [ValidateSet('GPT', 'MBR')]
        [System.String]
        $PartitionStyle = 'GPT',

        [Parameter()]
        [System.UInt64]
        $Size,

        [Parameter()]
        [System.String]
        $FSLabel,

        [Parameter()]
        [System.UInt32]
        $AllocationUnitSize,

        [Parameter()]
        [ValidateSet('NTFS', 'ReFS')]
        [System.String]
        $FSFormat = 'NTFS',

        [Parameter()]
        [System.Boolean]
        $AllowDestructive,

        [Parameter()]
        [System.Boolean]
        $ClearDisk
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingDiskMessage -f $DiskIdType, $DiskId, $DriveLetter)
        ) -join '' )

    # Validate the DriveLetter parameter
    $DriveLetter = Assert-DriveLetterValid -DriveLetter $DriveLetter

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckDiskInitializedMessage -f $DiskIdType, $DiskId)
        ) -join '' )

    # Get the Disk using the identifiers supplied
    $disk = Get-DiskByIdentifier `
        -DiskId $DiskId `
        -DiskIdType $DiskIdType

    if (-not $disk)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DiskNotFoundMessage -f $DiskIdType, $DiskId)
            ) -join '' )

        return $false
    } # if

    if ($disk.IsOffline)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DiskNotOnlineMessage -f $DiskIdType, $DiskId)
            ) -join '' )

        return $false
    } # if

    if ($disk.IsReadOnly)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DiskReadOnlyMessage `
                        -f $DiskIdType, $DiskId)
            ) -join '' )

        return $false
    } # if

    if ($disk.PartitionStyle -ne $PartitionStyle)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DiskPartitionStyleNotMatchMessage `
                        -f $DiskIdType, $DiskId, $disk.PartitionStyle, $PartitionStyle)
            ) -join '' )

        if ($disk.PartitionStyle -eq 'RAW' -or ($AllowDestructive -and $ClearDisk))
        {
            return $false
        }
        else
        {
            # This disk is initialized but with the incorrect partition style
            New-InvalidOperationException `
                -Message ($script:localizedData.DiskInitializedWithWrongPartitionStyleError `
                    -f $DiskIdType, $DiskId, $disk.PartitionStyle, $PartitionStyle)
        }
    } # if

    $partition = Get-Partition `
        -DriveLetter $DriveLetter `
        -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($partition.DriveLetter -ne $DriveLetter)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DriveLetterNotFoundMessage -f $DriveLetter)
            ) -join '' )

        return $false
    } # if

    # Check the partition size
    if ($partition -and -not ($PSBoundParameters.ContainsKey('Size')))
    {
        $supportedSize = ($partition | Get-PartitionSupportedSize)

        <#
            If the difference in size between the supported partition size
            and the current partition size is less than 1MB then set the
            desired partition size to the current size. This will prevent
            any size difference less than 1MB from trying to contiuously
            resize. See https://github.com/dsccommunity/StorageDsc/issues/181
        #>
        if (($supportedSize.SizeMax - $partition.Size) -lt 1MB)
        {
            $Size = $partition.Size
        }
        else
        {
            $Size = $supportedSize.SizeMax
        }
    }

    if ($Size)
    {
        if ($partition.Size -ne $Size)
        {
            # The partition size mismatches
            if ($AllowDestructive)
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.SizeMismatchWithAllowDestructiveMessage `
                                -f $DriveLetter, $Partition.Size, $Size)
                    ) -join '' )

                return $false
            }
            else
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.SizeMismatchMessage `
                                -f $DriveLetter, $Partition.Size, $Size)
                    ) -join '' )
            }
        } # if
    } # if

    $blockSize = (Get-CimInstance `
            -Query "SELECT BlockSize from Win32_Volume WHERE DriveLetter = '$($DriveLetter):'" `
            -ErrorAction SilentlyContinue).BlockSize

    if ($blockSize -gt 0 -and $AllocationUnitSize -ne 0)
    {
        if ($AllocationUnitSize -ne $blockSize)
        {
            # The allocation unit size mismatches
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.AllocationUnitSizeMismatchMessage `
                            -f $DriveLetter, $($blockSize.BlockSize / 1KB), $($AllocationUnitSize / 1KB))
                ) -join '' )

            if ($AllowDestructive)
            {
                return $false
            }
        } # if
    } # if

    # Get the volume so the properties can be checked
    $volume = Get-Volume `
        -DriveLetter $DriveLetter `
        -ErrorAction SilentlyContinue

    if ($PSBoundParameters.ContainsKey('FSFormat'))
    {
        # Check the filesystem format
        $fileSystem = $volume.FileSystem
        if ($fileSystem -ne $FSFormat)
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FileSystemFormatMismatch `
                            -f $DriveLetter, $fileSystem, $FSFormat)
                ) -join '' )

            if ($AllowDestructive)
            {
                return $false
            }
        } # if
    } # if

    if ($PSBoundParameters.ContainsKey('FSLabel'))
    {
        # Check the volume label
        $label = $volume.FileSystemLabel
        if ($label -ne $FSLabel)
        {
            # The assigned volume label is different and needs updating
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DriveLabelMismatch `
                            -f $DriveLetter, $label, $FSLabel)
                ) -join '' )

            return $false
        } # if
    } # if

    return $true
} # Test-TargetResource

Export-ModuleMember -Function *-TargetResource
