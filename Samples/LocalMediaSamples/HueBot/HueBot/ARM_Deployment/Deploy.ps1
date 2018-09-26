$subscriptionName="%replace_with_azure_subscription_name%"
$resourceGroupName="huebotsf02"
$keyvaultName="%replace_with_azure_keyvault_name%"
$parameterFilePath="%replace_with_path_to_repos_folder%\service-shared_platform_samples\LocalMediaSamples\HueBot\HueBot\ARM_Deployment\AzureDeploy.Parameters.json"
$templateFilePath="%replace_with_path_to_repos_folder%\service-shared_platform_samples\LocalMediaSamples\HueBot\HueBot\ARM_Deployment\AzureDeploy.json"
$secretID="%replace_with_secret_id_of_certificate_from_keyvault%"

Connect-AzureRmAccount
Select-AzureRmSubscription -SubscriptionName $subscriptionName

Set-AzureRmKeyVaultAccessPolicy -VaultName $keyvaultName -EnabledForDeployment
New-AzureRmServiceFabricCluster -ResourceGroupName $resourceGroupName -SecretIdentifier $secretId -TemplateFile $templateFilePath -ParameterFile $parameterFilePath
