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

    .PARAMETER AccessPath
        Specifies the access path folder to the assign the disk volume to

    .PARAMETER NoDefaultDriveLetter
        Prevents a default drive letter from being assigned to a newly created partition. Defaults to True.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER Size
        Specifies the size of new volume (use all available space on disk if not provided).

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $AccessPath,

        [Parameter()]
        [System.Boolean]
        $NoDefaultDriveLetter = $true,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

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
        $FSFormat = 'NTFS'
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingDiskMessage -f $DiskIdType, $DiskId, $AccessPath)
        ) -join '' )

    # Validate the AccessPath parameter adding a trailing slash
    $AccessPath = Assert-AccessPathValid -AccessPath $AccessPath -Slash

    # Get the Disk using the identifiers supplied
    $disk = Get-DiskByIdentifier `
        -DiskId $DiskId `
        -DiskIdType $DiskIdType

    # Get the partitions on the disk
    $partition = $disk | Get-Partition -ErrorAction SilentlyContinue

    # Check if the disk has an existing partition assigned to the access path
    $assignedPartition = $partition |
        Where-Object -Property AccessPaths -Contains -Value $AccessPath

    $volume = $assignedPartition | Get-Volume

    $fileSystem = $volume.FileSystem
    $FSLabel = $volume.FileSystemLabel

    # Prepare the AccessPath used in the CIM query (replaces '\' with '\\')
    $queryAccessPath = $AccessPath -replace '\\', '\\'

    $blockSize = (Get-CimInstance `
            -Query "SELECT BlockSize from Win32_Volume WHERE Name = '$queryAccessPath'" `
            -ErrorAction SilentlyContinue).BlockSize

    $returnValue = @{
        DiskId               = $DiskId
        DiskIdType           = $DiskIdType
        AccessPath           = $AccessPath
        NoDefaultDriveLetter = $assignedPartition.NoDefaultDriveLetter
        Size                 = $assignedPartition.Size
        FSLabel              = $FSLabel
        AllocationUnitSize   = $blockSize
        FSFormat             = $fileSystem
    }

    return $returnValue
} # Get-TargetResource

<#
    .SYNOPSIS
        Initializes the Disk and Partition and assigns the access path.

    .PARAMETER AccessPath
        Specifies the access path folder to the assign the disk volume to

    .PARAMETER NoDefaultDriveLetter
        Prevents a default drive letter from being assigned to a newly created partition. Defaults to True.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER Size
        Specifies the size of new volume (use all available space on disk if not provided).

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.
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
        $AccessPath,

        [Parameter()]
        [System.Boolean]
        $NoDefaultDriveLetter = $true,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

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
        $FSFormat = 'NTFS'
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingDiskMessage -f $DiskIdType, $DiskId, $AccessPath)
        ) -join '' )

    <#
        This call is required to force a refresh of the list of drives in
        the disk subsystem. If this refresh does not occur then the list of
        disks may not be up-to-date, resulting in an error occuring when the
        access path is mounted. The result of the test is not used and is discarded.
    #>
    $null = Test-AccessPathInPSDrive -AccessPath $AccessPath

    # Validate the AccessPath parameter adding a trailing slash
    $AccessPath = Assert-AccessPathValid -AccessPath $AccessPath -Slash

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

    switch ($disk.PartitionStyle)
    {
        'RAW'
        {
            # The disk partition table is not yet initialized, so initialize it with GPT
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.InitializingDiskMessage -f $DiskIdType, $DiskId)
                ) -join '' )

            $disk | Initialize-Disk `
                -PartitionStyle 'GPT'
            break
        } # 'RAW'

        'GPT'
        {
            # The disk partition is already initialized with GPT.
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DiskAlreadyInitializedMessage -f $DiskIdType, $DiskId)
                ) -join '' )
            break
        } # 'GPT'

        default
        {
            # This disk is initialized but not as GPT - so raise an exception.
            New-InvalidOperationException `
                -Message ($script:localizedData.DiskAlreadyInitializedError -f `
                    $DiskIdType, $DiskId, $Disk.PartitionStyle)
        } # default
    } # switch

    # Get the partitions on the disk
    $partition = $disk | Get-Partition -ErrorAction SilentlyContinue

    # Check if the disk has an existing partition assigned to the access path
    $assignedPartition = $partition |
        Where-Object -Property AccessPaths -Contains -Value $AccessPath

    if ($null -eq $assignedPartition)
    {
        # There is no partiton with this access path
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.AccessPathNotFoundOnPartitionMessage -f $DiskIdType, $DiskId, $AccessPath)
            ) -join '' )

        # Are there any partitions defined on this disk?
        if ($partition)
        {
            # There are partitions defined - identify if one matches the size required
            if ($Size)
            {
                # Find the first basic partition matching the size
                $partition = $partition |
                    Where-Object -Filter { $_.Type -eq 'Basic' -and $_.Size -eq $Size } |
                    Select-Object -First 1

                if ($partition)
                {
                    # A partition matching the required size was found
                    Write-Verbose -Message ($script:localizedData.MatchingPartitionFoundMessage -f `
                            $DiskIdType, $DiskId, $partition.PartitionNumber)
                }
                else
                {
                    # A partition matching the required size was not found
                    Write-Verbose -Message ($script:localizedData.MatchingPartitionNotFoundMessage -f `
                            $DiskIdType, $DiskId)
                } # if
            }
            else
            {
                <#
                    No size specified, so see if there is a partition that has a volume
                    matching the file system type that is not assigned to an access path.
                #>
                Write-Verbose -Message ($script:localizedData.MatchingPartitionNoSizeMessage -f `
                        $DiskIdType, $DiskId)

                $searchPartitions = $partition | Where-Object -FilterScript {
                    $_.Type -eq 'Basic' -and -not (Test-AccessPathAssignedToLocal -AccessPath $_.AccessPaths)
                }

                $partition = $null

                foreach ($searchPartition in $searchPartitions)
                {
                    # Look for the volume in the partition.
                    Write-Verbose -Message ($script:localizedData.SearchForVolumeMessage -f `
                            $DiskIdType, $DiskId, $searchPartition.PartitionNumber, $FSFormat)

                    $searchVolumes = $searchPartition | Get-Volume

                    $volumeMatch = $searchVolumes | Where-Object -FilterScript {
                        $_.FileSystem -eq $FSFormat
                    }

                    if ($volumeMatch)
                    {
                        <#
                            Found a partition with a volume that matches file system
                            type and not assigned an access path.
                        #>
                        $partition = $searchPartition

                        Write-Verbose -Message ($script:localizedData.VolumeFoundMessage -f `
                                $DiskIdType, $DiskId, $searchPartition.PartitionNumber, $FSFormat)

                        break
                    } # if
                } # foreach
            } # if
        } # if

        # Do we need to create a new partition?
        if (-not $partition)
        {
            # Attempt to create a new partition
            $partitionParams = @{}

            if ($Size)
            {
                # Use only a specific size
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.CreatingPartitionMessage `
                                -f $DiskIdType, $DiskId, "$($Size/1KB) KB")
                    ) -join '' )
                $partitionParams['Size'] = $Size
            }
            else
            {
                # Use the entire disk
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.CreatingPartitionMessage `
                                -f $DiskIdType, $DiskId, 'all free space')
                    ) -join '' )
                $partitionParams['UseMaximumSize'] = $true
            } # if

            # Create the partition
            $partition = $disk | New-Partition @partitionParams

            <#
                After creating the partition it can take a few seconds for it to become writeable
                Wait for up to 30 seconds for the partition to become writeable
            #>
            $timeout = (Get-Date) + (New-Timespan -Second 30)
            while ($partition.IsReadOnly -and (Get-Date) -lt $timeout)
            {
                Write-Verbose -Message ($script:localizedData.NewPartitionIsReadOnlyMessage -f `
                        $DiskIdType, $DiskId, $partition.PartitionNumber)

                Start-Sleep -Seconds 1

                # Pull the partition details again to check if it is readonly
                $partition = $partition | Get-Partition
            } # while
        } # if

        if ($partition.IsReadOnly)
        {
            # The partition is still readonly - throw an exception
            New-InvalidOperationException `
                -Message ($script:localizedData.NewParitionIsReadOnlyError -f `
                    $DiskIdType, $DiskId, $partition.PartitionNumber)
        } # if

        $assignAccessPath = $true
    }
    else
    {
        # The disk already has a partition on it that is assigned to the access path
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.PartitionAlreadyAssignedMessage -f `
                        $AccessPath, $assignedPartition.PartitionNumber)
            ) -join '' )

        $assignAccessPath = $false
    }

    # Get the Volume on the partition
    $volume = $partition | Get-Volume

    # Is the volume already formatted?
    if ($volume.FileSystem -eq '')
    {
        # The volume is not formatted
        $volParams = @{
            FileSystem = $FSFormat
            Confirm    = $false
        }

        if ($FSLabel)
        {
            # Set the File System label on the new volume
            $volParams["NewFileSystemLabel"] = $FSLabel
        } # if

        if ($AllocationUnitSize)
        {
            # Set the Allocation Unit Size on the new volume
            $volParams["AllocationUnitSize"] = $AllocationUnitSize
        } # if

        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.FormattingVolumeMessage -f $volParams.FileSystem)
            ) -join '' )

        # Format the volume
        $volume = $partition | Format-Volume @VolParams
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
                # There is nothing we can do to resolve this (yet)
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.FileSystemFormatMismatch -f `
                                $AccessPath, $fileSystem, $FSFormat)
                    ) -join '' )
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
                                -f $AccessPath, $FSLabel)
                    ) -join '' )

                $volume | Set-Volume -NewFileSystemLabel $FSLabel
            } # if
        } # if
    } # if

    # Assign the access path if it isn't assigned
    if ($assignAccessPath)
    {
        <#
            Add the partition access path, but do not pipe $disk to
            Add-PartitionAccessPath because it is not supported in
            Windows Server 2012 R2
        #>
        $null = Add-PartitionAccessPath `
            -AccessPath $AccessPath `
            -DiskNumber $disk.Number `
            -PartitionNumber $partition.PartitionNumber

        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.SuccessfullyInitializedMessage -f $AccessPath)
            ) -join '' )
    } # if

    # Get the partitions on the disk
    $partition = $disk | Get-Partition -ErrorAction SilentlyContinue

    # Get the current partition state for NoDefaultDriveLetter
    $assignedPartition = $partition |
        Where-Object -Property AccessPaths -Contains -Value $AccessPath

    if ($assignedPartition.NoDefaultDriveLetter -ne $NoDefaultDriveLetter)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                "$($script:localizedData.NoDefaultDriveLetterMismatchMessage -f $assignedPartition.NoDefaultDriveLetter, $NoDefaultDriveLetter)"
            ) -join '' )

        # Setting the partition property NoDefaultDriveLetter
        Set-Partition -PartitionNumber $assignedPartition.PartitionNumber `
            -DiskNumber $disk.Number `
            -NoDefaultDriveLetter $NoDefaultDriveLetter
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests if the disk is initialized, the partion exists and the access path is assigned.

    .PARAMETER AccessPath
        Specifies the access path folder to the assign the disk volume to

    .PARAMETER NoDefaultDriveLetter
        Prevents a default drive letter from being assigned to a newly created partition. Defaults to True.

    .PARAMETER DiskId
        Specifies the disk identifier for the disk to modify.

    .PARAMETER DiskIdType
        Specifies the identifier type the DiskId contains. Defaults to Number.

    .PARAMETER Size
        Specifies the size of new volume (use all available space on disk if not provided).

    .PARAMETER FSLabel
        Specifies the volume label to assign to the volume.

    .PARAMETER AllocationUnitSize
        Specifies the allocation unit size to use when formatting the volume.

    .PARAMETER FSFormat
        Specifies the file system format of the new volume.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $AccessPath,

        [Parameter()]
        [System.Boolean]
        $NoDefaultDriveLetter = $true,

        [Parameter(Mandatory = $true)]
        [System.String]
        $DiskId,

        [Parameter()]
        [ValidateSet('Number', 'UniqueId', 'Guid', 'Location')]
        [System.String]
        $DiskIdType = 'Number',

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
        $FSFormat = 'NTFS'
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingDiskMessage -f $DiskIdType, $DiskId, $AccessPath)
        ) -join '' )

    # Validate the AccessPath parameter adding a trailing slash
    $AccessPath = Assert-AccessPathValid -AccessPath $AccessPath -Slash

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
                $($script:localizedData.DiskReadOnlyMessage -f $DiskIdType, $DiskId)
            ) -join '' )
        return $false
    } # if

    if ($disk.PartitionStyle -ne 'GPT')
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DiskNotGPTMessage -f $DiskIdType, $DiskId, $Disk.PartitionStyle)
            ) -join '' )
        return $false
    } # if

    # Get the partitions on the disk
    $partition = $disk | Get-Partition -ErrorAction SilentlyContinue

    # Check if the disk has an existing partition assigned to the access path
    $assignedPartition = $partition |
        Where-Object -Property AccessPaths -Contains -Value $AccessPath

    if (-not $assignedPartition)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.AccessPathNotFoundMessage -f $AccessPath)
            ) -join '' )
        return $false
    } # if

    # Check if the partition NoDefaultDriveLetter parameter is correct
    if ($assignedPartition.NoDefaultDriveLetter -ne $NoDefaultDriveLetter)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NoDefaultDriveLetterMismatchMessage -f $assignedPartition.NoDefaultDriveLetter, $NoDefaultDriveLetter)
            ) -join '' )
        return $false
    } # if

    # Partition size was passed so check it
    if ($Size)
    {
        if ($assignedPartition.Size -ne $Size)
        {
            # The partition size mismatches but can't be changed (yet)
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.SizeMismatchMessage -f `
                            $AccessPath, $assignedPartition.Size, $Size)
                ) -join '' )
        } # if
    } # if

    # Prepare the AccessPath used in the CIM query (replaces '\' with '\\')
    $queryAccessPath = $AccessPath -replace '\\', '\\'

    $blockSize = (Get-CimInstance `
            -Query "SELECT BlockSize from Win32_Volume WHERE Name = '$queryAccessPath'" `
            -ErrorAction SilentlyContinue).BlockSize

    if ($blockSize -gt 0 -and $AllocationUnitSize -ne 0)
    {
        if ($AllocationUnitSize -ne $blockSize)
        {
            # The allocation unit size mismatches but can't be changed (yet)
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.AllocationUnitSizeMismatchMessage -f `
                            $AccessPath, $($blockSize.BlockSize / 1KB), $($AllocationUnitSize / 1KB))
                ) -join '' )
        } # if
    } # if

    # Get the volume so the properties can be checked
    $volume = $assignedPartition | Get-Volume

    if ($PSBoundParameters.ContainsKey('FSFormat'))
    {
        # Check the filesystem format
        $fileSystem = $volume.FileSystem
        if ($fileSystem -ne $FSFormat)
        {
            # The file system format does not match but can't be changed (yet)
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FileSystemFormatMismatch -f `
                            $AccessPath, $fileSystem, $FSFormat)
                ) -join '' )
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
                    $($script:localizedData.DriveLabelMismatch -f `
                            $AccessPAth, $label, $FSLabel)
                ) -join '' )
            return $false
        } # if
    } # if

    return $true
} # Test-TargetResource

<#
    .SYNOPSIS
        Check access path is found in PSDrives.

    .DESCRIPTION
        Check if the access path is found in the list of PSDrives and if not
        then forces a full refresh of the list and searches that.

    .PARAMETER AccessPath
        Specifies the access path folder.

    .NOTES
        The full refresh of PSDrive without any parameters is required because
        this is the only way to completely force a refresh of disks and other
        related CIM objects.

        If the refresh is not forced then there is a risk that the drive will
        appear to not be mounted, causing an error when the resource attempts
        to remount it.

        See https://github.com/dsccommunity/StorageDsc/issues/121
#>
function Test-AccessPathInPSDrive
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $AccessPath
    )

    try
    {
        $accessPathDisk = $AccessPath.Split(':')[0]
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.CheckingPSDriveMessage -f $AccessPath, $accessPathDisk)
            ) -join '' )

        $accessPathDrive = Get-PSDrive -Name $accessPathDisk -ErrorAction Stop
    }
    catch
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.UnavailablePSDriveMessage -f $AccessPath, $accessPathDisk)
            ) -join '' )

        $accessPathDrive = Get-PSDrive
        $accessPathDrive = $accessPathDrive | Where-Object -Property Name -eq $accessPathDisk
    }

    $accessPathFound = ($null -ne $accessPathDrive)

    if ($accessPathFound)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.PSDriveFoundMessage -f $AccessPath, $accessPathDisk)
            ) -join '' )
    }
    else
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.PSDriveNotFoundMessage -f $AccessPath, $accessPathDisk)
            ) -join '' )
    }

    return $accessPathFound
} # Test-AccessPathInPSDrive

Export-ModuleMember -Function *-TargetResource
