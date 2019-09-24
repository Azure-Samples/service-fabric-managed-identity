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

# Getting started with Managed Identity on Service Fabric

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master

Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master

Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

This walkthrough steps through the process of deploying a service fabric cluster to Azure with managed identity enabled, and then deploying an application that uses managed identity to that cluster.

## Prerequisites

- [You will need Powershell and the Az Powershell library to run this sample.](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps)
- You should have an Azure subscription and resource group you can write to. There is no way to test this sample locally.

## Overview

### Create a User-Assigned Managed Identity and Key Vault

1. [Create a user assigned managed identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal)
    - Make sure you create it in the region you would like your cluster to be in
2. Create a Key Vault and necessary cluster server and client certificates ([Below](###-step-2-create-a-key-vault-and-necessary-cluster-server-and-client-certificates))

### Deploy a cluster

3. Deploy a managed-identity-enabled cluster using an ARM template ([Below](###-step-3-deploy-a-managed-identity-enabled-cluster-using-an-arm-template
))

### Create an application

4. Create a storage account to host your application package and upload it ([Below](###-step-4-create-a-storage-account-to-host-your-application-package-and-upload-it))
5. Deploy an application
    a. Deploy an application with user-assigned managed identity
    b. Deploy code in a container with system-assigned managed identity

## Walkthrough

### Step 2: Create a Key Vault and necessary cluster server and client certificates
<!---
TODO: Provide an ARM template and deploy script to do this automatically
--->
#### Create Key Vault
1. Under your resource group, click `+ Add`, choose `Key Vault`, and click `Create`
2. Under `Basics`, fill out parameters, putting your keyvault in the same region that you would like your cluster to be in
3. Under `Access Policy`, check `Azure Virtual Machines for Deployment`, this is what allows ARM to access to keyvault to get the cluster certificate
4. Under `Access Policy`, click `+ Add Access Policy`
    - Under `Secret Permissions`, check `Get` and `List`
    - Under `Select Principal`, search and choose the managed identity you creared in Step 2
    - It should look like the below:

[key vault policy](/img/accesspolicy.png)

5. Click through to create the Key Vault

#### Create Cluster Certificate

1. Access your Key Vault in the Azure Portal and choose `Certificates` from the sidebar
2. Click `+ Generate/Import`
3. Enter as below: 
[certificate](/img/certificate.png)

### Step 3: Deploy a managed-identity-enabled cluster using an ARM template

1. Open `ResourceManagement/cluster.parameters.json` and complete the fields `clusterName`, `clusterLocation`, `adminPassword`, `sourceVaultValue`, `certificateUrlValue`, `certificateThumbprint`
    - You can find `sourceVaultValue` by navigating to your key vault in the Azure portal, choosing `Properties` from the sidebar, and copying the content under `Resource ID`
    - You can find `certificateUrlValue` and `certificatethumbprint` by navigating to your key vault, choosing `Certificates`, clicking into your certificate and then the current version, and copying the content under `Secret Identifier` and `X.509 SHA-1 Thumbprint` respectively.
2. Open `ResourceManagement/cluster.deploy.ps1` and complete `$Subscription`, `ResourceGroupName`, and `ResourceGroupLocation`
3. Start the deployment by running `.\cluster.deploy.ps1` from a Powershell window

### Step 4: Create a storage account to host your application package and upload it
<!---
TODO: Provide an ARM template and deploy script to do this automatically
--->
1. Under your resource group, click `+ Add`, choose `Storage account`, and click `Create`
2. Fill in required details, leaving the defaults for everything else. Make sure `Location` matches that of your cluster; click create
3. Once the deployment has completed, navigate to the Storage Account and choose `Blobs` from the sidebar. Choose `+ Container`, name it (this is where your app packages will be stored), and set `Public access level` to `Blob` (this will allow ARM to access during deployment)
4. Click into your container and choose `Upload`. Then, choose the corresponding sample package from the repo `package/` or from an application you have packaged yourself, and click upload.

### Step 5: Deploy an application

1. Open `ResourceManagement/app.parameters.json` and complete all the parameters
    - `aplicationPackageUrl` can be retrieved by navigating into the blob container that you created before, clicking into the application you would like to create, and copying the contents of `URL`.
2. Open `ResourceManagement/app.deploy.ps1` and complete `ResourceGroupName`
3. Start the deployment by running `.\app.deploy.ps1` from a Powershell window

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
