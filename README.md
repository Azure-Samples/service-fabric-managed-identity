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

```text
Note: All Azure resources used in the sample should be in the same region. This includes managed identity, Key Vault, Service Fabric cluster, and storage account.
```

- This sample requires access to an Azure subscription, as well as required privileges and roles to create and manage resources, as specified below.
- [To run the ARM deployments in the sample, Powershell and the Az Powershell library are needed.](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps)
- To deploy the user-assigned identity sample application, there needs to be a user-assigned managed identity created in Azure that can be assigned to it.
  - [How to create a user-assigned managed identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal)
- To deploy the cluster, there should be a Key Vault in Azure and a corresponding cluster certificate the resource provider can access. Access policy `Azure Virtual Machines for Deployment` needs to be checked for the ARM deployment to access the Key Vault.
  - [More information about certificates in Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/certificate-scenarios)
  - [Example self-signed cluster certificate](img/certificate.png)
- For a managed identity-enabled application to access Key Vault or any other Azure resource, the resource's access policies should be configured to allow access for the managed identity.
  - [More information about access policies in key vault](<https://docs.microsoft.com/en-us/azure/key-vault/key-vault-secure-your-key-vault>)
- To deploy a managed identity-enabled application via ARM, the application package should be in a storage account. `Public access level` of the container needs to be set to `Blob` for ARM to access the storage account during deployment.
  - [The first half of this document walks through how to upload an application package to a storage account](<https://docs.microsoft.com/en-us/azure/batch/batch-application-packages>)

## Sample Application Overview

Both samples use VaultProbe

### Console Application
<!-- TODO -->
### Web Application
<!-- TODO -->

## Walkthrough

### Deploy a managed-identity-enabled cluster

Deploying to Azure using the Azure Resource Manager is the recommended way of managing Azure resources. Provided is a sample cluster ARM template to create a Service Fabric cluster with managed identity enabled. The template uses the cluster certificate provided by your key vault, creates a system-assigned identity, and enables the Managed Identity token service so deployed applications can access their identities.

- [More information about ARM deployments](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-overview)
- [How to enable managed identity if you already have your own cluster template](https://docs.microsoft.com/en-us/azure/service-fabric/configure-new-azure-service-fabric-enable-managed-identity)

To use the provided template:

1. Open `ResourceManagement/cluster.parameters.json` and complete the fields `clusterLocation`, `adminUserName`, `adminPassword`, `sourceVaultValue`, `certificateUrlValue`, `certificateThumbprint`
    - `sourceVaultValue` can be found by navigating to your Key Vault in the Azure portal, choosing `Properties` from the sidebar, and copying the content under `Resource ID`
    - `certificateUrlValue` and `certificatethumbprint` can be found by navigating to your Key Vault, choosing `Certificates`, clicking into the cluster certificate and then the current version, and copying the content under `Secret Identifier` and `X.509 SHA-1 Thumbprint` respectively.
2. Open `ResourceManagement/cluster.deploy.ps1` and complete `$Subscription`, `ResourceGroupName`, and `ResourceGroupLocation`
3. Start the deployment by running `.\cluster.deploy.ps1` from a Powershell window

### Deploy an application with System or User Assigned Managed Identity

The provided ARM templates enable ARM to fetch the application package from storage and assign an identity to the deployed application.

- [How to enable system-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-system-assigned-managed-identity)
- [How to enable user-assigned identity on an existing application](https://docs.microsoft.com/en-us/azure/service-fabric/how-to-deploy-service-fabric-application-user-assigned-managed-identity)

To deploy an application with system-assigned identity, use the files with `system` prefix, and to deploy an application with user-assigned identity, use the files with `user` prefix.

1. Open `ResourceManagement/*.app.parameters.json` and complete all the parameters
    - `aplicationPackageUrl` can be found by navigating into the blob container containing your application in your storage account, clicking into the application you would like to create, and copying the contents of `URL`.
2. Open `ResourceManagement/*.app.deploy.ps1` and complete `ResourceGroupName`
3. Start the deployment by running `.\*.app.deploy.ps1` from a Powershell window

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
