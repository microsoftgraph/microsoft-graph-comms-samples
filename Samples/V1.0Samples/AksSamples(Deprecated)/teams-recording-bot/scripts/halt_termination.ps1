$continue = $true

$CallSignalingPort2 = [int]$env:AzureSettings__CallSignalingPort + 1

while($continue)
{
    try
    {
        $result = Invoke-WebRequest -Uri "http://localhost:$CallSignalingPort2/calls" -UseBasicParsing

        if ($result.Content)
        {
            Start-Sleep -Seconds 60
        }
        else
        {
            $continue = $false
        }
    }
    catch
    {
        "Error while calling endpoint."
    }
}
