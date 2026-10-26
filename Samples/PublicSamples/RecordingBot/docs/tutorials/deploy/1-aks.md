# Create an AKS cluster

Let us start with creating an AKS cluster, on which we will later deploy the containers for the
sample recording bot. Before we can start to run commands in the Azure command line tool, we need
to log in:

```powershell
az login --tenant 99999999-9999-9999-9999-999999999999
```

After running the command, we see the following message:

```text
The default web browser has been opened at https://login.microsoftonline.com/99999999-9999-9999-9999-999999999999/oauth2/v2.0/authorize. Please continue the login in the web browser. If no web browser is available or if the web browser fails to open, use device code flow with `az login --use-device-code`.
```

A browser window with the Microsoft login page should open, otherwise follow the advice of the output.

![Login Page](../../images/screenshot-login.png)

In the browser, we need to log in with our Microsoft Entra Id administrator account and accept the requested scopes.

After successful login, the output looks similar to this:

```json
[
  {
    "cloudName": "AzureCloud",
    "homeTenantId": "99999999-9999-9999-9999-999999999999",
    "id": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy",
    "isDefault": true,
    "managedByTenants": [],
    "name": "recordingbotsubscription",
    "state": "Enabled",
    "tenantId": "99999999-9999-9999-9999-999999999999",
    "user": {
      "name": "user@xyz.com",
      "type": "user"
    }
  }
]
```

## Create Azure Resource Group

Now we can start to create resources in our azure subscription.  
Let us start with creating a resource group.

```powershell
az group create 
    --location westeurope 
    --name recordingbottutorial 
    --subscription "recordingbotsubscription"
```

The result in the command line should look something like:

```json
{
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/recordingbottutorial",
  "location": "westeurope",
  "managedBy": null,
  "name": "recordingbottutorial",
  "properties": {
    "provisioningState": "Succeeded"
  },
  "tags": null,
  "type": "Microsoft.Resources/resourceGroups"
}
```

## Create Azure Kubernetes Cluster

With our Resource Group set up, we create an AKS cluster. For the purpose of this tutorial we will
use the Free Tier. To further reduce costs, we also set the node count of the system nodepool to 1.
Regarding the vm size of the system nodes in the system nodepool we choose the
`standard_d2s_v3`-series, as this series is available at the most regions and some nodes in this
series are available without requesting more quotas.

> [!WARNING]  
> Make sure to check the [pricing tiers](https://learn.microsoft.com/en-us/azure/aks/free-standard-pricing-tiers)
> and [pricing for VMs](https://azure.microsoft.com/en-us/pricing/details/virtual-machines/linux/)
> as costs may incur (even with the Free Tier). If you already have VMs running in your Azure
> subscribtion, also check your [per vm quotas](https://learn.microsoft.com/en-us/azure/quotas/per-vm-quota-requests)
> to avoid exceeding your quotas.

Here is the command we run to create our AKS cluster resource:

```powershell
az aks create
    --location westeurope
    --name recordingbotcluster
    --resource-group recordingbottutorial
    --tier free
    --node-count 1
    --node-vm-size standard_d2s_v3
    --network-plugin azure
    --no-ssh-key  
    --yes
    --subscription "recordingbotsubscription"
```

After waiting for the command to complete the output in our powershell should look similar to:

```json
Resource provider 'Microsoft.ContainerService' used by this operation is not registered. We are registering for you.
Registration succeeded.
{
  "aadProfile": null,
  "addonProfiles": null,
  "agentPoolProfiles": [
    {
      "availabilityZones": null,
      "capacityReservationGroupId": null,
      "count": 1,
      "creationData": null,
      "currentOrchestratorVersion": "1.28.5",
      "enableAutoScaling": false,
      "enableEncryptionAtHost": false,
      "enableFips": false,
      "enableNodePublicIp": false,
      "enableUltraSsd": false,
      "gpuInstanceProfile": null,
      "hostGroupId": null,
      "kubeletConfig": null,
      "kubeletDiskType": "OS",
      "linuxOsConfig": null,
      "maxCount": null,
      "maxPods": 30,
      "minCount": null,
      "mode": "System",
      "name": "nodepool1",
      "networkProfile": null,
      "nodeImageVersion": "AKSUbuntu-2204gen2containerd-202403.25.0",
      "nodeLabels": null,
      "nodePublicIpPrefixId": null,
      "nodeTaints": null,
      "orchestratorVersion": "1.28.5",
      "osDiskSizeGb": 128,
      "osDiskType": "Managed",
      "osSku": "Ubuntu",
      "osType": "Linux",
      "podSubnetId": null,
      "powerState": {
        "code": "Running"
      },
      "provisioningState": "Succeeded",
      "proximityPlacementGroupId": null,
      "scaleDownMode": null,
      "scaleSetEvictionPolicy": null,
      "scaleSetPriority": null,
      "spotMaxPrice": null,
      "tags": null,
      "type": "VirtualMachineScaleSets",
      "upgradeSettings": {
        "drainTimeoutInMinutes": null,
        "maxSurge": "10%",
        "nodeSoakDurationInMinutes": null
      },
      "vmSize": "standard_d2s_v3",
      "vnetSubnetId": null,
      "workloadRuntime": null
    }
  ],
  "apiServerAccessProfile": null,
  "autoScalerProfile": null,
  "autoUpgradeProfile": {
    "nodeOsUpgradeChannel": "NodeImage",
    "upgradeChannel": null
  },
  "azureMonitorProfile": null,
  "azurePortalFqdn": "recordingb-recordingbottuto-yyyyyy-1585fl53.portal.hcp.westeurope.azmk8s.io",
  "currentKubernetesVersion": "1.28.5",
  "disableLocalAccounts": false,
  "diskEncryptionSetId": null,
  "dnsPrefix": "recordingb-recordingbottuto-yyyyyy",
  "enablePodSecurityPolicy": null,
  "enableRbac": true,
  "extendedLocation": null,
  "fqdn": "recordingb-recordingbottuto-yyyyyy-1585fl53.hcp.westeurope.azmk8s.io",
  "fqdnSubdomain": null,
  "httpProxyConfig": null,
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourcegroups/recordingbottutorial/providers/Microsoft.ContainerService/managedClusters/recordingbotcluster",
  "identity": {
    "delegatedResources": null,
    "principalId": "51d916b6-0106-419f-825b-3d74e292559d",
    "tenantId": "99999999-9999-9999-9999-999999999999",
    "type": "SystemAssigned",
    "userAssignedIdentities": null
  },
  "identityProfile": {
    "kubeletidentity": {
      "clientId": "831201f7-171e-45d3-86a9-db8b87ded108",
      "objectId": "1aee7386-eb25-4ee0-91f8-cf4bf0641151",
      "resourceId": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourcegroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.ManagedIdentity/userAssignedIdentities/recordingbotcluster-agentpool"
    }
  },
  "ingressProfile": null,
  "kubernetesVersion": "1.28",
  "linuxProfile": null,
  "location": "westeurope",
  "maxAgentPools": 100,
  "name": "recordingbotcluster",
  "networkProfile": {
    "dnsServiceIp": "10.0.0.10",
    "ipFamilies": [
      "IPv4"
    ],
    "loadBalancerProfile": {
      "allocatedOutboundPorts": null,
      "backendPoolType": "nodeIPConfiguration",
      "effectiveOutboundIPs": [
        {
          "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.Network/publicIPAddresses/pppppppp-pppp-pppp-pppp-pppppppppppp",
          "resourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope"
        }
      ],
      "enableMultipleStandardLoadBalancers": null,
      "idleTimeoutInMinutes": null,
      "managedOutboundIPs": {
        "count": 1,
        "countIpv6": null
      },
      "outboundIPs": null,
      "outboundIpPrefixes": null
    },
    "loadBalancerSku": "standard",
    "natGatewayProfile": null,
    "networkDataplane": "azure",
    "networkMode": null,
    "networkPlugin": "azure",
    "networkPluginMode": null,
    "networkPolicy": null,
    "outboundType": "loadBalancer",
    "podCidr": null,
    "podCidrs": null,
    "serviceCidr": "10.0.0.0/16",
    "serviceCidrs": [
      "10.0.0.0/16"
    ]
  },
  "nodeResourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope",
  "oidcIssuerProfile": {
    "enabled": false,
    "issuerUrl": null
  },
  "podIdentityProfile": null,
  "powerState": {
    "code": "Running"
  },
  "privateFqdn": null,
  "privateLinkResources": null,
  "provisioningState": "Succeeded",
  "publicNetworkAccess": null,
  "resourceGroup": "recordingbottutorial",
  "resourceUid": "0123456789abcdef12345678",
  "securityProfile": {
    "azureKeyVaultKms": null,
    "defender": null,
    "imageCleaner": null,
    "workloadIdentity": null
  },
  "serviceMeshProfile": null,
  "servicePrincipalProfile": {
    "clientId": "msi",
    "secret": null
  },
  "sku": {
    "name": "Base",
    "tier": "Free"
  },
  "storageProfile": {
    "blobCsiDriver": null,
    "diskCsiDriver": {
      "enabled": true
    },
    "fileCsiDriver": {
      "enabled": true
    },
    "snapshotController": {
      "enabled": true
    }
  },
  "supportPlan": "KubernetesOfficial",
  "systemData": null,
  "tags": null,
  "type": "Microsoft.ContainerService/ManagedClusters",
  "upgradeSettings": null,
  "windowsProfile": {
    "adminPassword": null,
    "adminUsername": "azureuser",
    "enableCsiProxy": true,
    "gmsaProfile": null,
    "licenseType": null
  },
  "workloadAutoScalerProfile": {
    "keda": null,
    "verticalPodAutoscaler": null
  }
}
```

> [!NOTE]  
> We notice that the output of the command contains some extra resource we have not requested
> directly. These include a virtual machine scale set, a public IP and more. The extra resources
> have been placed in a newly created resource group. We will need the name of this resource group
> later, so let us search the output and note down its name. We can find it under the property
> `nodeResourceGroup`, among other places. In the provided example output, the resource group is
> called `MC_recordingbottutorial_recordingbotcluster_westeurope`.

If the command fails with the message:

```text
unrecognized arguments: --tier free
```

the az Azure command line tool is out of date.

## Add Windows Node Pool

In the previous section, we created an AKS cluster with a linux node. However, for our recording
bot application, we actually need a windows node instead. Let us create two new windows nodes of
the `standard_d4_v3`-series, this series is also available in most Microsoft Azure regions
and meets the requirement of two physical cpu cores specified by the media SDK:

```powershell
az aks nodepool add
    --cluster-name recordingbotcluster
    --name win22
    --resource-group recordingbottutorial
    --node-vm-size standard_d4_v3
    --node-count 2
    --os-type Windows
    --os-sku Windows2022 
    --subscription "recordingbotsubscription"
```

This command also needs some time to complete, our result output should look similar to:

```json
{
  "availabilityZones": null,
  "capacityReservationGroupId": null,
  "count": 2,
  "creationData": null,
  "currentOrchestratorVersion": "1.28.5",
  "enableAutoScaling": false,
  "enableEncryptionAtHost": false,
  "enableFips": false,
  "enableNodePublicIp": false,
  "enableUltraSsd": false,
  "gpuInstanceProfile": null,
  "hostGroupId": null,
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourcegroups/recordingbottutorial/providers/Microsoft.ContainerService/managedClusters/recordingbotcluster/agentPools/win22",
  "kubeletConfig": null,
  "kubeletDiskType": "OS",
  "linuxOsConfig": null,
  "maxCount": null,
  "maxPods": 30,
  "minCount": null,
  "mode": "User",
  "name": "win22",
  "networkProfile": null,
  "nodeImageVersion": "AKSWindows-2022-containerd-20348.2340.240401",
  "nodeLabels": null,
  "nodePublicIpPrefixId": null,
  "nodeTaints": null,
  "orchestratorVersion": "1.28.5",
  "osDiskSizeGb": 128,
  "osDiskType": "Managed",
  "osSku": "Windows2022",
  "osType": "Windows",
  "podSubnetId": null,
  "powerState": {
    "code": "Running"
  },
  "provisioningState": "Succeeded",
  "proximityPlacementGroupId": null,
  "resourceGroup": "recordingbottutorial",
  "scaleDownMode": "Delete",
  "scaleSetEvictionPolicy": null,
  "scaleSetPriority": null,
  "spotMaxPrice": null,
  "tags": null,
  "type": "Microsoft.ContainerService/managedClusters/agentPools",
  "typePropertiesType": "VirtualMachineScaleSets",
  "upgradeSettings": {
    "drainTimeoutInMinutes": null,
    "maxSurge": null,
    "nodeSoakDurationInMinutes": null
  },
  "vmSize": "standard_d4_v3",
  "vnetSubnetId": null,
  "workloadRuntime": null
}
```

Now our AKS cluster has 1 linux system node and 2 windows nodes for our recording application.

## Untaint system nodepool

Later we need to run some applications on our system nodes. But sometimes the system nodes have
_taints_ ([What are taints?](https://kubernetes.io/docs/concepts/scheduling-eviction/taint-and-toleration/)) that don't allow scheduling new pods, that means we can't run our
applications on the nodes.

### Check if system nodepool is tainted

So let's first check if our system nodepool has taints that would cause problems when scheduling.

```powershell
az aks nodepool list 
    --cluster-name recordingbotcluster
    --resource-group recordingbottutorial
    --subscription  "recordingbotsubscription" | Select-String 
            -Pattern 'name','nodeTaints','{','}','[',']','mode','NoSchedule' 
            -SimpleMatch 
            -NoEmphasis
```

The result of this should look like

```json
[
  {
    "mode": "System",
    "name": "nodepool1",
    "nodeTaints": [
      "CriticalAddonsOnly=true:NoSchedule"
    ],
    "powerState": {
    },
    "scaleDownMode": null,
    "upgradeSettings": {
    },
  },
  {
    "mode": "User",
    "name": "win22",
    "nodeTaints": null,
    "powerState": {
    },
    "scaleDownMode": "Delete",
    "upgradeSettings": {
    },
  }
]
```

The result will contain a list of nodepools of our aks cluster. To identify which nodepool we are
looking at, search for the _name_ field in the entry. In the example, we have a windows nodepool,
called `win22` and a system nodepool, called `nodepool1`.

> [!Note]  
> The _mode_ field of `nodepool1` shows `System` while the _mode_ field of the `win22` nodepool
> shows `User`.

To check if our system nodepool `nodepool1` is tainted check the _nodeTaints_ field. If the field
has a value of `null` we can continue with [setting the DNS name](#set-dns-name). If it's not the
case, like in our case, we have to untaint the nodepool. If your system nodepool has a different
name replace `nodepool1` with the name you see in your previous result.

```powershell
az aks nodepool update 
    --cluster-name recordingbotcluster
    --name nodepool1
    --resource-group recordingbottutorial
    --subscription "recordingbotsubscription"
    --%
    --node-taints ""
```

The result of the command should look similar to:

```json
{
  "availabilityZones": null,
  "capacityReservationGroupId": null,
  "count": 1,
  "creationData": null,
  "currentOrchestratorVersion": "1.28.5",
  "enableAutoScaling": false,
  "enableEncryptionAtHost": false,
  "enableFips": false,
  "enableNodePublicIp": false,
  "enableUltraSsd": false,
  "gpuInstanceProfile": null,
  "hostGroupId": null,
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourcegroups/recordingbottutorial/providers/Microsoft.ContainerService/managedClusters/recordingbotcluster/agentPools/nodepool1",
  "kubeletConfig": null,
  "kubeletDiskType": "OS",
  "linuxOsConfig": null,
  "maxCount": null,
  "maxPods": 30,
  "minCount": null,
  "mode": "System",
  "name": "nodepool1",
  "networkProfile": null,
  "nodeImageVersion": "AKSUbuntu-2204gen2containerd-202403.25.0",
  "nodeLabels": null,
  "nodePublicIpPrefixId": null,
  "nodeTaints": null,
  "orchestratorVersion": "1.28.5",
  "osDiskSizeGb": 128,
  "osDiskType": "Managed",
  "osSku": "Ubuntu",
  "osType": "Linux",
  "podSubnetId": null,
  "powerState": {
    "code": "Running"
  },
  "provisioningState": "Succeeded",
  "proximityPlacementGroupId": null,
  "resourceGroup": "recordingbottutorial",
  "scaleDownMode": null,
  "scaleSetEvictionPolicy": null,
  "scaleSetPriority": null,
  "spotMaxPrice": null,
  "tags": null,
  "type": "Microsoft.ContainerService/managedClusters/agentPools",
  "typePropertiesType": "VirtualMachineScaleSets",
  "upgradeSettings": {
    "drainTimeoutInMinutes": null,
    "maxSurge": "10%",
    "nodeSoakDurationInMinutes": null
  },
  "vmSize": "standard_d2s_v3",
  "vnetSubnetId": null,
  "workloadRuntime": null
}
```

As you can see the _nodeTaints_ field now updated to `null`.

## Set DNS name

When we created our AKS cluster, a public IP was automatically created for us. To use this IP
address, we want to create a DNS entry so we can reference it by name. Since the IP address was
automatically created for us, it resides in the automatically created resource group of managed
resources. If we didn't note down the name of this resource group in the
[Create Azure Kubernetes Cluster](#create-azure-kubernetes-cluster) step, follow the next step to find out the name. Otherwise we
can skip ahead to [getting the public IP resource name](#get-the-public-ip-resource).

### Get the managed resources resource group name

We can find the managed resource group in the description of the AKS cluster:

```powershell
az aks show
    --resource-group recordingbottutorial
    --name recordingbotcluster
    --subscription "recordingbotsubscription" | Select-String 
            -Pattern 'nodeResourceGroup' 
            -SimpleMatch
            -NoEmphasis
```

This will give us the response

```json
  "nodeResourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope",
```

The IP address resource was created in the Azure managed resource group which is used by Azure to
manage the AKS cluster. You can find the managed resource group name in the _nodeResourceGroup_
field of the output. In the example output the resource group is called
`MC_recordingbottutorial_recordingbotcluster_westeurope`.

### Get the public IP resource

Since the public IP address is an Azure resource, it can be referenced by name. To find out, what
name was assigned to it, we execute the following command:

```powershell
az network public-ip list 
    --resource-group MC_recordingbottutorial_recordingbotcluster_westeurope
    --subscription "recordingbotsubscription"
```

We consider our example output:

```json
[
  {
    "ddosSettings": {
      "protectionMode": "VirtualNetworkInherited"
    },
    "etag": "W/\"f5508f05-0e65-479a-9399-d436f02e0a66\"",
    "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.Network/publicIPAddresses/pppppppp-pppp-pppp-pppp-pppppppppppp",
    "idleTimeoutInMinutes": 4,
    "ipAddress": "255.255.255.255",
    "ipConfiguration": {
      "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.Network/loadBalancers/kubernetes/frontendIPConfigurations/pppppppp-pppp-pppp-pppp-pppppppppppp",
      "resourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope"
    },
    "ipTags": [],
    "location": "westeurope",
    "name": "pppppppp-pppp-pppp-pppp-pppppppppppp",
    "provisioningState": "Succeeded",
    "publicIPAddressVersion": "IPv4",
    "publicIPAllocationMethod": "Static",
    "resourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope",
    "resourceGuid": "539f439e-5c50-4bc6-a4a9-fb2d102e88f3",
    "sku": {
      "name": "Standard",
      "tier": "Regional"
    },
    "tags": {
      "aks-managed-cluster-name": "recordingbotcluster",
      "aks-managed-cluster-rg": "recordingbottutorial",
      "aks-managed-type": "aks-slb-managed-outbound-ip"
    },
    "type": "Microsoft.Network/publicIPAddresses",
    "zones": [
      "3",
      "1",
      "2"
    ]
  }
]
```

The name of the public IP Address resource can be found in the _name_ field of the output. In our
example output the name of the public IP address resource is `pppppppp-pppp-pppp-pppp-pppppppppppp`.
Also write down the value of the _ipAddress_ field as we need the the address when we deploy the sample
to our aks cluster, in our example output the ipAddress is `255.255.255.255`.

### Set DNS name for public IP resource

With the managed resource group name and the public IP resource name, we can now set a DNS name for
the public IP resource with the command:

```powershell
az network public-ip update 
    --resource-group MC_recordingbottutorial_recordingbotcluster_westeurope 
    --name pppppppp-pppp-pppp-pppp-pppppppppppp
    --dns-name recordingbottutorial
    --subscription "recordingbotsubscription"
```

Do not forget to replace the DNS name with your own AKS DNS record. We do not use the fully
qualified name. So in our example, for the DNS entry
`recordingbottutorial`_.westeurope.cloudapp.azure.com_ we supply the parameter `--dns-name`
only with `recordingbottutorial`.

> [!WARNING]  
> The DNS record that is created from our custom part and the postfix must be globally unique.

If the command completed successful the result will look similar to this:

```json
{
  "ddosSettings": {
    "protectionMode": "VirtualNetworkInherited"
  },
  "dnsSettings": {
    "domainNameLabel": "recordingbottutorial",
    "fqdn": "recordingbottutorial.westeurope.cloudapp.azure.com"
  },
  "etag": "W/\"dc9d1467-e11a-46fa-bf7d-ad60a7713c7f\"",
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.Network/publicIPAddresses/pppppppp-pppp-pppp-pppp-pppppppppppp",
  "idleTimeoutInMinutes": 4,
  "ipAddress": "108.141.184.42",
  "ipConfiguration": {
    "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/MC_recordingbottutorial_recordingbotcluster_westeurope/providers/Microsoft.Network/loadBalancers/kubernetes/frontendIPConfigurations/pppppppp-pppp-pppp-pppp-pppppppppppp",
    "resourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope"
  },
  "ipTags": [],
  "location": "westeurope",
  "name": "pppppppp-pppp-pppp-pppp-pppppppppppp",
  "provisioningState": "Succeeded",
  "publicIPAddressVersion": "IPv4",
  "publicIPAllocationMethod": "Static",
  "resourceGroup": "MC_recordingbottutorial_recordingbotcluster_westeurope",
  "resourceGuid": "539f439e-5c50-4bc6-a4a9-fb2d102e88f3",
  "sku": {
    "name": "Standard",
    "tier": "Regional"
  },
  "tags": {
    "aks-managed-cluster-name": "recordingbotcluster",
    "aks-managed-cluster-rg": "recordingbottutorial",
    "aks-managed-type": "aks-slb-managed-outbound-ip"
  },
  "type": "Microsoft.Network/publicIPAddresses",
  "zones": [
    "3",
    "1",
    "2"
  ]
}
```

As we can see, the field _dnsSettings_ was added to the resource. Within the DNS settings object
(the field _dnsSettings_) we can see the custom part of our DNS record (field _domainNameLabel_)
and the fully qualified domain name (field _fqdn_) with the postfix provided by Azure.

If the fqdn already exists we would get the follwing error message.

```text
(DnsRecordInUse) DNS record recordingbottutorial.westeurope.cloudapp.azure.com is already used byanother public IP.
Code: DnsRecordInUse
Message: DNS record recordingbottutorial.westeurope.cloudapp.azure.com is already used by another public IP.
```

then we would have to come up with a new prefix for the DNS record.

## Install kubectl tool

If we have the tool already installed we can skip this part and [get the credentials for our aks cluster](#get-aks-credentials).

We can install kubectl with the Azure command line tool:

```powershell
az aks install-cli
```

The resulting output should look similar to

```text
The detected architecture of current device is "amd64", and the binary for "amd64" will be downloaded. If the detection is wrong, please download and install the binary corresponding to the appropriate architecture.
No version specified, will get the latest version of kubectl from "https://storage.googleapis.com/kubernetes-release/release/stable.txt"
Downloading client to "C:\Users\User\.azure-kubectl\kubectl.exe" from "https://storage.googleapis.com/kubernetes-release/release/v1.29.4/bin/windows/amd64/kubectl.exe"
No version specified, will get the latest version of kubelogin from "https://api.github.com/repos/Azure/kubelogin/releases/latest"
Downloading client to "C:\Users\User\AppData\Local\Temp\tmp56tfm9jk\kubelogin.zip" from "https://github.com/Azure/kubelogin/releases/download/v0.1.1/kubelogin.zip"
Moving binary to "C:\Users\User\.azure-kubelogin\kubelogin.exe" from "C:\Users\User\AppData\Local\Temp\tmp56tfm9jk\bin\windows_amd64\kubelogin.exe"
```

> [!NOTE]  
> After installing kubectl on our kubernetes cluster, we might need to open a new powershell
> session and may need to log in again as described in [Create an AKS cluster](#create-an-aks-cluster).

## Get AKS credentials

To deploy resources to our AKS cluster and do the operations with the kubernetes command line
tool (kubectl), we need access to our cluster. We can get the credentials for kubectl with the
azure command line tool:

```powershell
az aks get-credentials 
    --name recordingbotcluster
    --resource-group recordingbottutorial
    --subscription "recordingbotsubscription"
```

A successful result will look like:

```text
Merged "recordingbotcluster" as current context in C:\Users\User\.kube\config
```

To test if it really was successful, we will try to list the nodes in our kubernetes cluster with kubectl

```powershell
kubectl get nodes
```

If the run was successful the result will look like:

```text
NAME                                STATUS   ROLES   AGE     VERSION
aks-nodepool1-18840134-vmss000001   Ready    agent   4h31m   v1.28.5
akswin22000002                      Ready    agent   4h29m   v1.28.5
akswin22000003                      Ready    agent   4h29m   v1.28.5
```

> [!TIP]  
> If you experience problems with kubectl and did not install kubectl with the azure command line
> tool, try to [install kubectl with azure command line tool](#install-kubectl-tool).

As a small summary we now:

- created an AKS cluster
- added a windows nodepool with 2 nodes
- made sure we can use the system nodes
- set up a DNS record into the cluster
- retrieved the credentials for deployments to our AKS cluster

Next we can [create an Azure container registry](./2-acr.md).
