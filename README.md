---
page_type: sample
languages:
- csharp
products:
- dotnet
description: "End-to-end walkthrough of deploying a service fabric application with managed identities."
urlFragment: service-fabric-managed-identity

author: athinanthny, amenarde
ms.author: atsenthi, anmenard
service: service-fabric
---

# Getting started with Managed Identity for Service Fabric Applications

This document walks through the process of deploying a service fabric cluster to Azure with managed identity enabled, and then deploying an application that has a managed identity to that cluster. The provided sample application uses that identity to access secrets in an Azure Key Vault.

[Read more about managed identity on Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/concepts-managed-identity)

## Environment Requirements

> **Note:** All Azure resources used in the sample should be in the same region & resource group. This includes managed identity, Key Vault, Service Fabric cluster, and storage account.

- This sample requires access to an Azure subscription and required privileges to create resources
- [Powershell and the Az library are needed to run the deployments in the sample.](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps)
- Docker is needed to build and push the sample containerized service. When testing locally, Docker should be using Windows containers, not linux containers.

## Setting up Prerequisite Resources

From an elevated powershell window, run
```powershell
Connect-AzAccount
Select-AzSubscription -Subscription $Subscription
# If you do not already have a resource group to create resources from this walkthrough:
New-AzResourceGroup -Name $ResourceGroupName -Location $Location
```

You can create the below tabled resources yourself, or use the provided ARM template to create these resources for you by opening `ResourceManagement\prerequisites.parameters.json` and filling out all the fields, then starting the deployment by running from a Powershell window: 
```powershell
 New-AzResourceGroupDeployment -TemplateParameterFile ".\prerequisites.parameters.json" -TemplateFile ".\prerequisites.template.json" -ResourceGroupName $ResourceGroupName
```

| Resource | Description |
| :--- | :--- |
| User-Assigned Managed Identity | This identity will be assigned to the service fabric application |
| Key Vault with identity given access | This keyvault will hold the cluster certificate and will be accessed by the sample application. Access policy `Azure Virtual Machines for Deployment` needs to be checked. [The Key Vault's access policies should be configured to allow access for the managed identity](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-secure-your-key-vault) |
| Storage Account with Blob container| To deploy the application via ARM, [the application package should be in a storage account](<https://docs.microsoft.com/en-us/azure/batch/batch-application-packages>). `Public access level` of the container needs to be set to `Blob` for ARM to access the storage account during deployment. |
| Container registry to host console service| For Service Fabric to pull the containerized `MISampleConsole` service, it needs to be hosted in a container registry. Specific build instructions are in the walkthrough. [More information on creating a containerized application in Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started-containers). |

### Create a cluster certificate

To deploy the cluster, a cluster certificate needs to be in Key Vault at deployment time. You can create a self-signed certificate in the portal or by running:
```powershell
$Policy = New-AzKeyVaultCertificatePolicy -SubjectName $SubjectName -IssuerName Self -ValidityInMonths 12
Add-AzKeyVaultCertificate -VaultName $VaultName -Name $CertName -CertificatePolicy $Policy
```

## Walkthrough

### Deploy a managed-identity-enabled cluster

Deploying to Azure using the Azure Resource Manager is the recommended way of managing Azure resources. Provided is a sample cluster ARM template to create a Service Fabric cluster with managed identity enabled. The template uses the cluster certificate provided by your key vault, creates a system-assigned identity, and enables the Managed Identity token service so deployed applications can access their identities.

To use the provided template:

1. Open [cluster.parameters.json](ResourceManagement/cluster.parameters.json) and complete the fields 
    - `clusterLocation`, `adminUserName`, `adminPassword`, `sourceVaultValue`, `certificateUrlValue`, `certificateThumbprint`
1. In [cluster.parameters.json](ResourceManagement/cluster.parameters.json), manually, or using `ctrl-f` and replace-all, change all instances of `mi-test` to a unique name, like `myusername-mi-test`. This will help ensure the deployment resource names do not conflict with the names of other public resources.
1. Start the deployment by running from a Powershell window: 
```powershell
 New-AzResourceGroupDeployment -TemplateParameterFile ".\cluster.parameters.json" -TemplateFile ".\cluster.template.json" -ResourceGroupName $ResourceGroupName
```

### Deploy the sample application

#### Application and Service Definition Changes

1. In [ApplicationManifest.xml](MISampleApp/ApplicationPackageRoot/ApplicationManifest.xml)
    1. Complete `RepositoryCredentials` with the credentials for your container registry
    2. Complete `Vault_URI` with the uri of your vault (e.g. https://mykv.vault.azure.net)
    1. Complete `Secret_Name` with the name of your secret
1. In [ServiceManifest.xml](MISampleApp/ApplicationPackageRoot/MISampleConsolePkg/ServiceManifest.xml), complete `ImageName` under `ContainerHost` with the container repository address the image will be hosted at.
1. In [build.ps1](MISampleApp/SFMISampleConsole/build.ps1), enter your desired image name (e.g. same as address)

#### Packaging and building your application

1. [Package `MISampleApp` and upload it to your storage account.](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-package-apps)
2. Build the console app by running [.\build.ps1](MISampleApp/SFMISampleConsole/build.ps1) from an elevated powershell window.
    1. Please note docker should be running, and with Windows containers
1. [Push the image to your private Azure Container Repository](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli), e.g.
    1. docker login mycr.azurecr.io
    1. docker push mycr.azurecr.io/folder/image

#### Deploy sample app

1. Open [app.parameters.json](ResourceManagement/app.parameters.json) and complete 
    - `clusterName`, `applicationPackageURL`, and `userAssignedIdentityName`
    - `aplicationPackageUrl` can be found by navigating into the blob container containing your application in your storage account, clicking into the application you would like to create, and copying the contents of `URL`.
1. Start the deployment by running from a Powershell window: 
```powershell
 New-AzResourceGroupDeployment -TemplateParameterFile ".\app.parameters.json" -TemplateFile ".\app.template.json" -ResourceGroupName $ResourceGroupName
```

3. Before the application starts running, the console app will not have access to a key vault. The application's system assigned identity is created only after the application is deployed. Once the application is deployed, the system assigned identity can be given access permission to keyvault. The system assigned identity's name is {cluster}/{application name}/{servicename}

## Monitoring Secrets

| MISampleApp Service | Service type |Managed identity it uses | How to validate it is working |
| :--- | :--- | :--- | :--- |
| MISampleWeb | ASP.NET Core App |User-Assigned | Go to the public endpoint `http://mycluster.myregion.cloudapp.azure.com:80/vault` |
| MISampleConsole | Containerized C# Console App |System-Assigned | Remote into node running the service at `my.node.ip:3389`, find the running container with command `docker ps`, and look at the logs with `docker logs` |

## Next Steps

- [Learn more about accessing Key Vault using service fabric and a managed identity](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-managed-identity-service-fabric-app-code#accessing-key-vault-from-a-service-fabric-application-using-managed-identity)
- [How to enable managed identity if you already have your own cluster template](https://docs.microsoft.com/en-us/azure/service-fabric/configure-new-azure-service-fabric-enable-managed-identity)
- [How to enable system-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-system-assigned-managed-identity)
- [How to enable user-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-user-assigned-managed-identity)


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
