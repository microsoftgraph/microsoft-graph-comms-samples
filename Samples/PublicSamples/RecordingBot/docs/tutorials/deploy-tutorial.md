# Deploy Tutorial

In this tutorial we learn how to deploy the recording bot sample to a new AKS Cluster. We will also
set up a Recording Policy for all users within our Tenant and see the compliance redording banner
in our teams client.

## Prerequisites

- [Windows 11](https://www.microsoft.com/software-download/windows11)
- [Powershell 7 as administrator](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows)
- [Git command line tool](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)
- [AZ Azure command line tool](https://learn.microsoft.com/cli/azure/install-azure-cli-windows)
- [Helm command line tool](https://helm.sh/docs/intro/install/)
- [kubectl command line tool](https://kubernetes.io/docs/tasks/tools/install-kubectl-windows/)
  - This tutorial also shows how to install kubectl with the azure command line tool
- [Microsoft Entra Id Tenant](https://learn.microsoft.com/entra/fundamentals/create-new-tenant) [with Microsoft Teams users](https://learn.microsoft.com/entra/fundamentals/license-users-groups)
- [Microsoft Azure Subscription](https://learn.microsoft.com/azure/cost-management-billing/manage/create-subscription)
  - The subscription in this tutorial is called `recordingbotsubscription`, also see [variables](#variables).
- Microsoft Entra Id adminstrator

The Microsoft Entra Id administrator is required to create recording policies and to approve
application permissions of the app registration. This tutorial assumes we are a Microsoft Entra Id
administrator and always log in as such unless the tutorial requires otherwise.

## Contents

1. [Create an AKS cluster](./deploy/1-aks.md)
2. [Create an Azure Container Registry](./deploy/2-acr.md)
3. [Clone and build recording bot sample](./deploy/3-build.md)
4. [Create and configure Bot Service](./deploy/4-bot-service.md)
5. [Deploy recording sample to AKS cluster](./deploy/5-helm.md)
6. [Create and assign a Recording Policy](./deploy/6-policy.md)
7. [Verify functionality](./deploy/7-test.md)

## Variables

Throughout this tutorial we will create azure resources. The names we choose in this tutorial are:

|         Resource         |                              Name                              |
| ------------------------ | -------------------------------------------------------------- |
| Resource Group           | `recordingbottutorial`                                         |
| AKS Cluster              | `recordingbotcluster`                                          |
| Azure Container Registry | `recordingbotregistry`                                         |
| App Registration         | `recordingbotregistration`                                     |
| Bot Service              | `recordingbotservice`                                          |
| Azure Subscription       | `recordingbotsubscription`                                     |
| Public IP Address        | _pppppppp-pppp-pppp-pppp-pppppppppppp_                         |
| Managed Resource Group   | _MC__`recordingbottutorial`_`recordingbotcluster`__westeurope_ |

Variables that are used in this tutorial are:

|                        What?                        |                          Value                          |
| --------------------------------------------------- | ------------------------------------------------------- |
| Recording Bot Name                                  | `Tutorial Bot`                                          |
| AKS DNS record                                      | `recordingbottutorial`_.westeurope.cloudapp.azure.com_  |
| App Registration Id                                 | _cccccccc-cccc-cccc-cccc-cccccccccccc_                  |  
| App Registration Secret                             | _abcdefghijklmnopqrstuvwxyz_                            |
| Recording Policy Name                               | `TutorialPolicy`                                        |
| Recording Policy Application Instance UPN           | `tutorialbot@lm-ag.de`                                  |
| Recording Policy Application Instance Display Name  | `Tutorial Bot`                                          |
| Recording Policy Application Instance Object Id     | _11111111-1111-1111-1111-111111111111_                  |
| Microsoft Entra Id Tenant Id                        | _99999999-9999-9999-9999-999999999999_                  |
| Kubernetes Recording Bot Deployment Name            | `recordingbottutorial`                                  |
| Kubernetes Recording Bot Namespace                  | `recordingbottutorial`                                  |
| Let's Encrypt Email address                         | `tls-security@lm-ag.de`                                 |
| Windows Nodepool                                    | `win22`                                                 |
| Azure Subscription Id                               | _yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy_                 |
| Azure Region                                        | `westeurope`                                            |
| Directory for source code                           | `C:\Users\User\recordingbottutorial\`                   |
| Recording Application Docker Container Tag          | `recordingbottutorial/application:latest`               |
| Public IP of the Public IP Address Resource         | _255.255.255.255_                                       |

> [!TIP]  
> Consider to define own variable values before we start. Keep in mind the Azure resources have
> limitations for naming, read [this](https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules) for reference. Some Values are automatically generated and
> can't be changed, but needs to be replaced with you're custom values.

If you encounter any problems during the tutorial, please feel free to create an [issue](https://github.com/lm-development/aks-sample/issues).
This means that the tutorial can be continuously expanded to include error handling.

Now let us start [create an AKS cluster](./deploy/1-aks.md)
