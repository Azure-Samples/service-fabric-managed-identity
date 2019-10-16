---
page_type: sample
languages:
- csharp
products:
- dotnet
description: "Samples showcasing how to used Managed Identity service with Service Fabric."
urlFragment: service-fabric-managed-identity

author: athinanthny, amenarde
ms.author: atsenthi, anmenard
service: service-fabric
---

# Getting started with Managed Identity for Service Fabric Applications

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master
Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master
Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

This document walks through the process of deploying a service fabric cluster to Azure with managed identity enabled, and then deploying an application that has a managed identity to that cluster. The provided sample application uses that identity to access secrets in an Azure Key Vault.

[Read more about managed identity on Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/concepts-managed-identity)

## Prerequisites

> Note: All Azure resources used in the sample should be in the same region & resource group. This includes managed identity, Key Vault, Service Fabric cluster, and storage account.

- This sample requires access to an Azure subscription, as well as required privileges and roles to create and manage resources, as specified below.
- [Powershell and the Az library are needed to run the deployments in the sample.](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps)
- Docker is needed to build and push the sample containerized service. When testing locally, Docker should be using Windows containers, not linux containers.
- To deploy the user-assigned identity sample application, there should exist [a user-assigned managed identity created in Azure](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal) that can be assigned to the application.
- To deploy the cluster, there should be a Key Vault in Azure and a [corresponding cluster certificate](img/certificate.png) the resource provider can access. Access policy `Azure Virtual Machines for Deployment` needs to be checked for the ARM deployment to access the Key Vault.
- For a managed identity-enabled application to access Key Vault (or any other Azure resource), [the Key Vault's access policies should be configured to allow access for the managed identity](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-secure-your-key-vault).
- To deploy a managed identity-enabled application via ARM, [the sample application package should be in a storage account](<https://docs.microsoft.com/en-us/azure/batch/batch-application-packages>). `Public access level` of the container needs to be set to `Blob` for ARM to access the storage account during deployment.
- For Service Fabric to pull the containerized `MISampleConsole` service, it needs to be hosted in a container registry. Specific build instructions are in the walkthrough. [More information on creating a containerized application in Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started-containers).

## Sample Application Overview

| MISampleApp Service | Service type |Managed identity it uses | How to validate it is working |
| :--- | :--- | :--- | :--- |
| MISampleWeb | ASP.NET Core App |User-Assigned | Go to the public endpoint `http://mycluster.myregion.cloudapp.azure.com:80/vault` |
| MISampleConsole | Containerized C# Console App |System-Assigned | Remote into node running the service at `my.node.ip:3389`, find the running container with command `docker ps`, and look at the logs with `docker logs` |

> [Learn more about accessing Key Vault using service fabric and a managed identity](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-managed-identity-service-fabric-app-code#accessing-key-vault-from-a-service-fabric-application-using-managed-identity)

## Walkthrough

### Deploy a managed-identity-enabled cluster

Deploying to Azure using the Azure Resource Manager is the recommended way of managing Azure resources. Provided is a sample cluster ARM template to create a Service Fabric cluster with managed identity enabled. The template uses the cluster certificate provided by your key vault, creates a system-assigned identity, and enables the Managed Identity token service so deployed applications can access their identities.

- [More information about ARM deployments](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-overview)
- [How to enable managed identity if you already have your own cluster template](https://docs.microsoft.com/en-us/azure/service-fabric/configure-new-azure-service-fabric-enable-managed-identity)

To use the provided template:

1. Open `ResourceManagement/cluster.parameters.json` and complete the fields `clusterLocation`, `adminUserName`, `adminPassword`, `sourceVaultValue`, `certificateUrlValue`, `certificateThumbprint`
    - `sourceVaultValue` can be found by navigating to your Key Vault in the Azure portal, choosing `Properties` from the sidebar, and copying the content under `Resource ID`
    - `certificateUrlValue` and `certificatethumbprint` can be found by navigating to your Key Vault, choosing `Certificates`, clicking into the cluster certificate and then the current version, and copying the content under `Secret Identifier` and `X.509 SHA-1 Thumbprint` respectively.
2. In `ResourceManagement/cluster.parameters.json`, manually, or using `ctrl-f` and replace-all, change all instances of `mi-test` to a unique name, like `myusername-mi-test`. This will help ensure the deployment resource names do not conflict with the names of other public resources.
3. Open `ResourceManagement/cluster.deploy.ps1` and complete `Subscription`, `ResourceGroupName`, and `ResourceGroupLocation`
4. Start the deployment by running `.\cluster.deploy.ps1` from a Powershell window

### Deploy the sample application

#### Application and Service Definition Changes

1. In `MISampleApp/ApplicationPackageRoot/ApplicationManifest.xml`, complete `RepositoryCredentials` with the credentials for your container registry.
2. In `MISampleApp/ApplicationPackageRoot/MISampleConsolePkg/ServiceManifest.xml`, complete `sfmi_observed_vault` and `sfmi_observed_Secret` under `EnvironmentVariables` with the information of the secret you would like the console app to access. Complete `ImageName` under `ContainerHost` with the container repository address the image will be hosted at.
3. In `MISampleWb/PackageRoot/ServiceManifest.xml`, complete `sfmi_observed_vault` and `sfmi_observed_Secret` under `EnvironmentVariables` with the information of the secret the web app should access.
4. In `SFMISampleConsole/build.ps1`, enter your desired image name.

#### Packaging and building your application

1. [Package `MISampleApp` and upload it to your storage account.](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-package-apps)
2. Build the console app by running `.\build.ps1` in `SFMISampleConsole` from an elevated powershell window.
3. [Push the image to your private Azure Container Repository](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-docker-cli)


#### Deploy sample app

- [How to enable system-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-system-assigned-managed-identity)
- [How to enable user-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-user-assigned-managed-identity)

1. Open `ResourceManagement/app.parameters.json` and complete `clusterName`, `applicationPackageURL`, and `userAssignedIdentityName`.
    - `aplicationPackageUrl` can be found by navigating into the blob container containing your application in your storage account, clicking into the application you would like to create, and copying the contents of `URL`.
2. Start the deployment by running from a Powershell window: 
```
New-AzResourceGroupDeployment -ResourceGroupName <resourcegroup> -TemplateParameterFile ".\app.parameters.json" -TemplateFile ".\app.template.json" -verbose
```

3. Before the application starts running, the console app will not have access to a key vault. The application's system assigned identity is created only after the application is deployed. Once the application is deployed, the system assigned identity can be given access permission to keyvault. The system assigned identity's name is {cluster}/{application name}/{servicename}

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
