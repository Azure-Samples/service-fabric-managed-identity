# See here for compatible Windows OS versions: 
# https://docs.microsoft.com/virtualization/windowscontainers/deploy-containers/version-compatibility#windows-10-version-1809-host-os-compatibility
FROM mcr.microsoft.com/windows/nanoserver:1809
ADD bin/release/netcoreapp2.1/win10-x64/publish/ /
ENTRYPOINT SFMISampleConsole.exe
