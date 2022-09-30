# Nager.FirewallManagement

With this small application you can manage your Windows Firewall via a WebApi, the WebApi is protected by an ApiKey (Header `X-Api-Key`).
In the current implementation a remote desktop connection can be allowed for a specific Ip address. Currently we have the logic for RDP and MSSQL available. If you need more you can send a pull request.

## Download
- You can download the binaries [here](https://github.com/Nager/Nager.FirewallManagement/releases/latest/download/Nager.FirewallManagement.zip)

## Configuration
The `ApiKey` can set in the `appsettings.json`,  the default `ApiKey` is `changeme`.

## Installation

With the following command you can register the windows service

```
sc.exe create "Nager.FirewallManagement" start=auto binPath="C:\Tools\Nager.FirewallManagement\Nager.FirewallManagement.exe"
```

## Uninstallation

With the following command you can unregister the windows service

```
sc.exe delete "Nager.FirewallManagement"
```

## How to use the WebApi

After the installation and start of the Windows service you can reach the WebApi under the following Url `http://localhost:5000/swagger`
