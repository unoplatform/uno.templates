# Uno.Templates

The `dotnet new` templates for [Uno Platform](https://platform.uno): one C# and XAML codebase, running on Android, iOS, WebAssembly, Windows, macOS and Linux. This repo also builds the `Uno.Sdk` that those templates sit on.

[![NuGet Uno.Templates](https://img.shields.io/nuget/v/Uno.Templates?label=Uno.Templates&logo=nuget)](https://www.nuget.org/packages/Uno.Templates)
[![NuGet Uno.Sdk](https://img.shields.io/nuget/v/Uno.Sdk?label=Uno.Sdk&logo=nuget)](https://www.nuget.org/packages/Uno.Sdk)
[![CI](https://github.com/unoplatform/uno.templates/actions/workflows/ci.yml/badge.svg)](https://github.com/unoplatform/uno.templates/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/unoplatform/uno.templates)](LICENSE.md)

## Quick start

```bash
dotnet new install Uno.Templates
dotnet new unoapp -o MyApp
```

That gives you a blank multi-platform app. Want the full "production-ready" setup (navigation, DI, configuration, localization, HTTP, logging, tests...)? Add `-preset recommended`.

New to Uno Platform? The [Get Started guide](https://aka.platform.uno/get-started) walks you through installing everything you need. You can also create projects from the IDE:

- [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=unoplatform.uno-platform-addin-2022)
- Visual Studio Code and Rider extensions (see the Get Started guide)

The IDE wizards use the same templates as the command line.

## Templates

| Short name | Name | What it is |
|---|---|---|
| `unoapp` | Uno Platform App | The main solution template: a multi-platform app targeting Android, iOS, WebAssembly, Skia Desktop (Windows, macOS, Linux) and Windows App SDK. |
| `unolib` | Uno Platform Class Library | A cross-targeted class library for Uno Platform (WinUI, Android, iOS, WebAssembly and Skia). |
| `unomauilib` | Uno Platform Maui Embedding Class Library | A library for .NET MAUI controls that you embed inside an Uno Platform app. |
| `unoapp-uitest` | Uno Platform UI Tests Class Library | A project for UI tests of an Uno Platform app. |

## Options

Run `dotnet new unoapp --help` for the full list. These are the ones you will reach for most:

| Option | Alias | Values | Default |
|---|---|---|---|
| `--preset` | `-preset` | `blank`, `recommended` | `blank` |
| `--tfm` | `-tfm` | `net10.0`, `net11.0` | `net10.0` |
| `--platforms` | `-platforms` | `android`, `ios`, `wasm`, `desktop`, `windows` (repeatable) | `android`, `ios`, `wasm`, `desktop` |
| `--renderer` | `-renderer` | `skia`, `native` | `skia` |
| `--presentation` | `-presentation` | `none`, `mvvm`, `mvux` | depends on preset |
| `--markup` | `-markup` | `xaml`, `csharp` | `xaml` |
| `--app-theme` | `-theme` | `material`, `fluent`, `cupertino`, `simple` | depends on preset |
| `--navigation` | `-nav` | `regions`, `blank` | depends on preset |
| `--authentication` | `-auth` | `none`, `custom`, `msal`, `oidc`, `Web` | `none` |
| `--use-http` | `-http` | `none`, `basic`, `refit`, `kiota` | depends on preset |
| `--logging` | `-log` | `none`, `default`, `serilog` | depends on preset |
| `--tests` | `-tests` | `none`, `unit`, `ui` | depends on preset |
| `--server` | `-server` | `true`, `false` | `false` |
| `--toolkit` | `-toolkit` | `true`, `false` | depends on preset |
| `--dsp` | `-dsp` | `true`, `false` | depends on preset |
| `--continuous-integration` | `-ci` | `none`, `azure`, `github` | `none` |
| `--skip-restore` | `-skip` | `true`, `false` | `false` |

There are plenty more (dependency injection, configuration, localization and cultures, theme service, SVG, Lottie, MAUI embedding, PWA manifest, VS Code and Rider files...). Defaults marked "depends on preset" change when you pick `-preset recommended`.

The other templates are much smaller. They all take `--tfm`; `unolib` also takes `--renderer` and `--global-json`, and `unomauilib` takes `--android`, `--ios` and `--winappsdk`.

### Examples

```bash
# Recommended preset, MVVM instead of MVUX
dotnet new unoapp -o MyApp -preset recommended -presentation mvvm

# C# Markup with the Fluent theme and the Uno Toolkit
dotnet new unoapp -o MyApp -preset recommended -markup csharp -theme fluent -toolkit

# Desktop and WebAssembly only, native renderer
dotnet new unoapp -o MyApp -platforms desktop wasm --renderer native

# Recommended app with a server, Kiota HTTP client and OIDC auth
dotnet new unoapp -o MyApp -preset recommended -server -http kiota -auth oidc

# A class library
dotnet new unolib -o MyLibrary
```

## What's in this repo

| Path | What lives there |
|---|---|
| [`src/Uno.Templates`](src/Uno.Templates) | The template pack project (`Uno.Templates.csproj`). The templates themselves are under `content/` (`unoapp`, `unolib`, `unolib-uitest`, `unomauilib`). `reinstall.ps1` builds and installs the pack locally. |
| [`src/Uno.Sdk`](src/Uno.Sdk) | The `Uno.Sdk` MSBuild SDK package, including `packages.json`, the version manifest. |
| [`src/Uno.Templates.sln`](src/Uno.Templates.sln) | Solution for the projects above. |
| [`tools/Uno.Sdk.Updater`](tools/Uno.Sdk.Updater) | The tool that bumps package versions in `packages.json` and friends. |
| [`tools/TemplateTfmSwitchGenerator`](tools/TemplateTfmSwitchGenerator) | Generates the per-TFM switch cases used by the template configuration. |
| [`build`](build) | PowerShell scripts for CI checks (stable pins, NuGet.org dependency verification, signing, Windows SDK install) and updater config in `build/templates`. |
| [`specs`](specs) | Design specs for template changes. |
| [`.github`](.github) | CI workflows and the composite actions they use. |
| `version.json` | [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) configuration. |

## Uno.Sdk

The templates generate projects that use `Uno.Sdk` (the "Uno Platform Single Project"), and this repo builds and ships that package too. The SDK takes care of the platform heads and the versions of the commonly used NuGet packages, so your project file stays small.

Package versions come from [`src/Uno.Sdk/packages.json`](src/Uno.Sdk/packages.json), a manifest of package groups (Uno core, Extensions, Toolkit, Themes, WASM Bootstrap, SkiaSharp, WinAppSDK, AndroidX, MAUI...). Nobody edits it by hand for routine bumps:

- The **Update Uno Sdk** workflow ([`uno-updater.yml`](.github/workflows/uno-updater.yml)) runs every 6 hours, uses [`tools/Uno.Sdk.Updater`](tools/Uno.Sdk.Updater) to refresh the manifest for `main` and the supported release branches, and opens a pull request with auto-merge enabled.
- [`uno-updater-pr.yml`](.github/workflows/uno-updater-pr.yml) runs the same update on PRs that touch the updater itself.
- On stable release branches, CI rejects prerelease pins (`build/Verify-StablePins.ps1`).

If you need a different version in your own app, you can override it with an MSBuild property. The [Uno.Sdk ReadMe](src/Uno.Sdk/ReadMe.md) lists every property and its current default.

## Building and testing locally

You need a .NET SDK (CI builds the packages with 9.0.300). To also build the apps the templates generate, follow the [Get Started guide](https://aka.platform.uno/get-started).

### Build and install the templates

The quick way, from `src/Uno.Templates` (PowerShell):

```powershell
./reinstall.ps1
```

It uninstalls any installed `Uno.Templates` (and legacy template packs), cleans `bin`/`obj`, builds the pack as version `255.255.255.255` and runs `dotnet new install` on the resulting `.nupkg`.

Or do it by hand:

```bash
dotnet build src/Uno.Templates/Uno.Templates.csproj -c Release -p:PackageVersion=255.255.255.255
dotnet new uninstall Uno.Templates
dotnet new install src/Uno.Templates/bin/Release/Uno.Templates.255.255.255.255.nupkg
```

CI does the same for both packages (`dotnet build -c Release -p:PackageVersion=...`, see [`build-packages`](.github/actions/ci/build-packages/action.yml)). Building `src/Uno.Sdk/Uno.Sdk.csproj` works the same way, but it first downloads the SDK content from Uno's package feed, so it needs network access to it.

### Try a template

```bash
dotnet new unoapp -o ../TestApp -preset recommended
cd ../TestApp
dotnet build
```

Pass `-skip` (`--skip-restore`) to skip the restore when you only want to look at the generated files.

### What CI does

[`ci.yml`](.github/workflows/ci.yml) runs on pushes and PRs to `main`, `servicing/*` and `release/*`, plus a nightly canary run:

1. Verifies there are no prerelease pins on stable release branches.
2. Builds the `Uno.Sdk` and `Uno.Templates` packages.
3. Signs them, and publishes dev builds from `main` (production publishing sits behind approval gates).
4. Generates a test matrix ([`generate-test-matrix`](.github/actions/ci/generate-test-matrix/action.yml)): a big list of `dotnet new` argument combinations (presets, platforms, markup, MVVM/MVUX, HTTP flavors, auth modes, themes, MAUI embedding, libraries...).
5. Runs that matrix on Linux, Windows and macOS: install the freshly built templates, create each project, and build it ([`run-tests`](.github/actions/ci/run-tests/action.yml)).

If you add or change an option, add a matching entry to the matrix.

## Contributing

Contributions are welcome. Please:

- Read the [Code of Conduct](CODE_OF_CONDUCT.md).
- Report security issues as described in [SECURITY.md](SECURITY.md), not in a public issue.
- Use [Conventional Commits](https://www.conventionalcommits.org) for your commits (`feat: ...`, `fix: ...`, `docs: ...`). The [conventional-commits](.github/workflows/conventional-commits.yml) check validates every PR and fails if they do not match.

## License

Licensed under the [Apache License 2.0](LICENSE.md).
