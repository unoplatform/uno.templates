# Spec: Aspire AppHost

## Summary

Add an **Aspire** option to the `unoapp` template. Selecting it adds an Aspire AppHost project that starts the app, and the Server project when there is one, with the Aspire dashboard for their logs, traces and metrics. The app calls the Server by its Aspire name instead of a fixed address.

## Motivation

`Uno.Extensions.Aspire` (the app side) and `Uno.Extensions.Aspire.Hosting` (the AppHost side) let an Uno Platform app run under Aspire. The `Uno.Sdk` gets an `Aspire` UnoFeature for the app side, but a feature only changes the project that lists it: it cannot add the AppHost to the solution. Without the template, users create the AppHost, the Server's service defaults and the app's settings by hand.

## Template parameters

| Symbol | Type | Change |
|---|---|---|
| `aspire` | `bool` | New parameter, **Aspire**, default `false` |
| `useAspire` | computed | `aspire && useDependencyInjection`, with the `desktop` or `wasm` platform |
| `useAspireServer` | computed | `useAspire && server` |

The option needs dependency injection: `UseServiceDefaults()` is a host builder extension. `--aspire` turns dependency injection on unless `--dependencyInjection false` is given, in which case it is ignored. It also needs the `desktop` or `wasm` platform, the heads the AppHost starts; without one, it is ignored. It is not part of the `blank` or `recommended` presets.

The option does not need the Server: without one, the AppHost starts the app alone and the dashboard still shows its logs, traces and metrics.

## What gets stamped

| Project or file | When | Notes |
|---|---|---|
| `MyExtensionsApp.1.AppHost` | `useAspire` | `AppHost.cs`, `https` and `http` launch profiles on ports generated per project, as Aspire's own template does. Adds the app with `AddUnoProject`, a desktop head when `desktop` is a platform, and a WebAssembly head when `wasm` is, started from the dashboard. With the Server, adds it as `api`, gives each head a reference to it, and passes the WebAssembly head's origin to it. |
| `aspire.config.json` | `useAspire` | Points `aspire run` at the AppHost. |
| `MyExtensionsApp.1.ServiceDefaults` | `useAspireServer` | Aspire's standard service defaults: OpenTelemetry, health checks, service discovery, resilience. |
| IDE run configurations | `useAspire` | An AppHost launch target in Rider (`rider`) and VS Code (`vscode`), using the `https` profile. |

Mobile and Windows heads are not added: the Android emulator needs a network security config for the emulator's host address, and the Windows head and the iOS simulator head are not released yet. The app still runs on those platforms from the IDE.

Changes to existing projects:

- **App:** `Aspire` in `UnoFeatures`, `.UseServiceDefaults()` on the host builder. With the Server, `ApiClient:Url` in `appsettings.development.json` is `https+http://api`, the Server's Aspire name, and `services:api` holds the Server's local address: a head started from the AppHost gets the Server's address from it, and one started from the IDE, as the mobile and Windows heads are, calls the local address as before.
- **Server:** with Aspire, references ServiceDefaults, calls `AddServiceDefaults()` and `MapDefaultEndpoints()`, and with Serilog forwards its logs to the other logging providers so they reach the dashboard. With the WebAssembly platform, it also allows the origin the AppHost passes in `Cors:Origin`, and no other.

## Versions

- `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`: `$(AspireVersion)` in `src/Directory.Build.targets`.
- `Uno.Extensions.Aspire.Hosting`: the AppHost is not an `Uno.Sdk` project, so it takes `$(UnoExtensionsVersion)`, which `Uno.Sdk.Updater` writes from the `Extensions` group of the `Uno.Sdk` manifest. The AppHost and the app's `Uno.Extensions.Aspire` are on the same version when the template is created; after an `Uno.Sdk` update in `global.json`, the AppHost's package is updated by hand, as the generated `Directory.Packages.props` says.
- ServiceDefaults packages: `$(AspireServiceDefaultsExtensionsVersion)` and `$(AspireServiceDefaultsOpenTelemetryVersion)` in `src/Directory.Build.targets`, the versions the `aspire-servicedefaults` template of `$(AspireVersion)` uses. They are updated with `AspireVersion`.

## Wizard

`TemplateWizard.json`:

- An **Aspire** card in the Features section, after **Server** (`/Assets/Features.Aspire.svg`, in the four theme variants).
- It requires **Dependency Injection** and the **Desktop** or **WebAssembly** platform.

## Prerequisites

The template references packages that are not released yet:

- The `Aspire` UnoFeature in the `Uno.Sdk`.
- `Uno.Extensions.Aspire` and `Uno.Extensions.Aspire.Hosting`.

## Validation

- `build/Test-WizardOptions.ps1` and `build/Test-WizardIcons.ps1` pass.
- Generated with `-preset recommended -server -aspire -platforms desktop wasm`, built with no warnings, and run from the AppHost: the desktop and WebAssembly heads called the Server by name, and the trace showed both the app and the Server.
- CI builds `-aspire` with and without the Server. Those scenarios fail until the prerequisites are released.
