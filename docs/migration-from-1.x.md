# Moving from Atc.Installer 1.x

AtcSoft Deployment Manager 2.0 is the next version of **Atc.Installer** (`atc-net/atc-installer`), under a new name, on .NET 10. This page lists what changes and how to move a server over.

> 🚧 This guide grows with each change to the project file format. Check it again before you move a server.

## In short

1. Install the .NET 10 Desktop Runtime.
2. Install `AtcSoft.Deployment.Manager.Installer.msi` from the [latest release](https://github.com/atc-net-apps/atc-soft-deployment-manager/releases/latest). It installs **next to** Atc.Installer 1.x; it doesn't replace it. Atc.Installer 1.x doesn't tell you about 2.0: its update check only looks at the old repo.
3. Copy each project folder from 1.x to the new data folder and check its files (see below).
4. Open each project in 2.0 and compare it with 1.x.
5. When every project works in 2.0, uninstall Atc.Installer 1.x.

## What changes

| | Atc.Installer 1.x | AtcSoft Deployment Manager 2.0 |
|---|---|---|
| Install folder | `C:\Program Files\ATC\Atc.Installer` | `C:\Program Files\AtcSoft\Deployment Manager` |
| Runtime | .NET 8 Desktop Runtime | .NET 10 Desktop Runtime |
| Projects | `%ProgramData%\ATC\atc-installer\Projects` | `%ProgramData%\AtcSoft\AtcSoft.Deployment.Manager\Projects` |
| Logs | `%ProgramData%\ATC\atc-installer\Logs` | `%ProgramData%\AtcSoft\AtcSoft.Deployment.Manager\Logs` |
| Recent files list | `%ProgramData%\ATC\atc-installer` (shared by all users) | `%LocalAppData%\AtcSoft\AtcSoft.Deployment.Manager` (per user) |
| App settings backup (`appsettings.custom.json`) | `%ProgramData%\ATC\atc-installer` | `%ProgramData%\AtcSoft\AtcSoft.Deployment.Manager` |
| Downloads and unpacked files | `%TEMP%\atc-installer` | `%TEMP%\AtcSoft.Deployment.Manager` (or the folder in the settings) |
| Updates | From `atc-net/atc-installer` | From `atc-net-apps/atc-soft-deployment-manager` |

2.0 doesn't copy anything from the 1.x folders. The installer creates the new data folder with write access for all users of the server.

## Moving a project

1. Copy the project folder, e.g. `%ProgramData%\ATC\atc-installer\Projects\MyProject`, to `%ProgramData%\AtcSoft\AtcSoft.Deployment.Manager\Projects\MyProject`.
2. Check `InstallationSettings.json`, `CustomSettings.json` and `TemplateSettings.json` for the changes below.
3. If the project downloads from Azure: update the template settings in the blob container once every server that uses it runs 2.0, or use a new container (or blob names) for 2.0 while servers still run 1.x.

## Project file changes

| Change | What to do |
|---|---|
| `"HostingFramework": "DonNetFramework48"` (old spelling) doesn't load, in 1.x since the spelling was fixed or in 2.0 | Use `"DotNetFramework48"` |
| `"ComponentEndpointType"` in `Endpoints` was never read; endpoint types fell back to the default | Use `"EndpointType"` |
| `InternetInformationServiceSettings` and `DefaultCredentials` were never read | Put the IIS ports (`HttpPort`, `HttpsPort`) in `ApplicationSettings` |

No other format changes yet.

## How 1.x merges CustomSettings into TemplateSettings

2.0 does the same for now. The tests in `test/AtcSoft.Deployment.Manager.Tests/ProjectFiles/ConfigurationFileHelperTests.cs` describe it:

- `Azure` comes from CustomSettings.
- A `DefaultApplicationSettings` key in CustomSettings overwrites the template's value, but only when the template has the key.
- Per application (matched by name): application settings, folder permissions, registry settings, firewall rules and JSON settings are added only when the template doesn't have them. An application that is only in CustomSettings is ignored.
- An XML setting is added when one of its attribute values isn't found in **any** XML setting of the same file. It is matched by value, not by key.