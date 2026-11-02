# Changelog

## [2.0.0](https://github.com/atc-net-apps/atc-soft-deployment-manager/compare/v1.0.26...v2.0.0) (2026-11-02)


### ⚠ BREAKING CHANGES

* the 1.x data in %ProgramData%\ATC\atc-installer is not copied; the migration guide explains how to move a project.
* needs the .NET 10 Desktop Runtime.

### New features

* move to .NET 10 and Atc.Wpf 4.3 ([6e9bbd1](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/6e9bbd186d8bce9af5c59ad7446d5d02dcaa4286))
* new data folders and a safer startup ([525286b](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/525286bd81d2997c62a976544c09d2c9e6aa100b))
* update check against the new repository ([5849d37](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/5849d375ff92aee43710cbe0d45bb8cbc78f3cec))


### Bug fixes

* the inactivity check no longer keeps a CPU core busy ([97c15f8](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/97c15f8b83f7477505f01ba47fd35aed220d3039))
* the skip variable also stops the update dialog's check ([fd50eec](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/fd50eec481ca2cfc1c37fd5dfdedb91d4ce13d01))


### Refactorings

* restructure into AtcSoft.Deployment.Manager projects ([b9a4bb1](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/b9a4bb1c6f7f9f77e91e6c4ba4bf83cdee80130a))


### Documentation

* 1.x doesn't announce 2.0, so the migration guide says so ([af42ee6](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/af42ee672233f322dd3828e2555871fe0c67f815))
* README for the new name, and the start of the 1.x migration guide ([38730ae](https://github.com/atc-net-apps/atc-soft-deployment-manager/commit/38730ae428a8b150b0559fb1267bff59086eebf4))
