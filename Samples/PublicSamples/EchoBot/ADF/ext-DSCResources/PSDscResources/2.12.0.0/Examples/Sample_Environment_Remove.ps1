<#
    .SYNOPSIS
        Removes the environment variable 'TestEnvironmentVariable' from
        both the machine and the process.
#>
Configuration Sample_Environment_Remove 
{
    param ()

    Import-DscResource -ModuleName 'PSDscResources'

    Node localhost
    {
        Environment RemoveEnvironmentVariable
        {
            Name = 'TestEnvironmentVariable'
            Ensure = 'Absent'
            Path = $false
            Target = @('Process', 'Machine')
        }
    }
}

Sample_Environment_Remove
