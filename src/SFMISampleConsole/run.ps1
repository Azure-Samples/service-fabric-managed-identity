param([string]$mode="env:cert")

# Docker image name for the application
$ImageName="sfmisampleconsole"

function Invoke-Docker-Run ([string]$imageName, [string]$parameter) {
	echo "Running test for $mode"
	Invoke-Expression "docker run --rm $imageName $parameter"
}

Invoke-Docker-Run -DockerImage $ImageName $mode