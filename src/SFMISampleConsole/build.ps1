<#
.SYNOPSIS
   Build the Console Application and Docker Image for Windows container
.DESCRIPTION
    Build the Console Application and Docker Image for Windows container
#>
param([string]$imageName = "davsftest.azurecr.io/sfmisamples/sfmisampleconsole", 
	[string]$dockerFile = "dockerfile-nanosrv")

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

function Invoke-DotNet ([string]$dotNetPath, [string]$dotNetParameters) {
    Invoke-Expression "$dotNetPath $dotNetParameters"
}

function Invoke-Docker-Build ([string]$ImageName, [string]$dockerFile, [string]$ImagePath, [string]$DockerBuildArgs = "") {
    echo "docker build -t $ImageName $ImagePath $DockerBuildArgs"
    Invoke-Expression "docker build -t $ImageName -f $dockerFile $ImagePath $dockerBuildArgs" 
}

Invoke-DotNet -DotNetPath "dotnet.exe" -DotNetParameters "build .\SFMISampleConsole.csproj -c release -r win10-x64"
Invoke-DotNet -DotNetPath "dotnet.exe" -DotNetParameters "publish -c release -r win10-x64 --self-contained"
Invoke-Docker-Build -ImageName $ImageName -dockerFile $dockerFile -ImagePath "." 