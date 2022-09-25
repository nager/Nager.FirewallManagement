# Nager.FirewallManagement

With this small application you can manage your Windows Firewall via a WebApi, the WebApi is protected by an ApiKey (Header `X-Api-Key`).
In the current implementation a remote desktop connection can be allowed for a specific Ip address.

## Configuration
The ApiKey can set in the `appsettings.json`.

## Installation

With the following command you can register the windows service

```
sc.exe create "Nager.FirewallManagement" start=auto binPath="C:\Tools\Nager.FirewallManagement\Nager.FirewallManagement.exe"
```

## Uninstallation

```
sc.exe delete "Nager.FirewallManagement"
```
