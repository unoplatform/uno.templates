# Spec: tvOS platform

## Summary

Add **tvOS** as a choice of the `unoapp` template's `platforms` parameter. Selecting it adds the `$(tfm)-tvos` target framework to the app head and stamps a `Platforms/tvOS` folder, so an Uno Platform app can target Apple TV without any manual project setup.

## Motivation

Uno Platform 7.0 supports tvOS 15 and above through `net10.0-tvos` (Skia rendering on the AppleUIKit runtime, see unoplatform/uno#24041), and the `Uno.Sdk` already handles `net*-tvos` heads (`Uno.Common.tvOS.targets`, `Uno.SingleProject.tvOS.targets`, the AppleUIKit runtime and native asset packages). The only missing piece is the template: today users have to add the TFM and the platform folder by hand.

## Template parameters

| Symbol | Type | Change |
|---|---|---|
| `platforms` | `choice` | New `tvos` choice, displayed as **tvOS** ("Enables the Apple TV target platform") |

tvOS is **not** part of the default `platforms` value nor of the `blank` / `recommended` presets: it needs the `tvos` workload, which is not installed by default, and most apps do not target the TV. It is opt-in, like the Android TV / Auto / Wear form factors.

The `$SingleProjectTFMs$` switch is regenerated with `tools/TemplateTfmSwitchGenerator`, which now places `-tvos` right after `-ios` for both `net10.0` and `net11.0`.

## What gets stamped

`Platforms/tvOS` is excluded unless `platforms` contains `tvos`. It mirrors `Platforms/iOS`, minus what does not apply to the TV:

| File | Notes |
|---|---|
| `Main.tvOS.cs` | Same `UnoPlatformHostBuilder ... .UseAppleUIKit()` entry point as iOS. The `.tvOS.cs` suffix is compiled for tvOS only by the `Uno.Sdk`. |
| `Info.plist` | `UIDeviceFamily` `3` (Apple TV) and `arm64`. No orientation, status bar, `LSRequiresIPhoneOS` or app icon keys. `MinimumOSVersion` is left to the `Uno.Sdk` (`SupportedOSPlatformVersion` defaults to `15.0` for tvOS), as iOS does. |
| `Entitlements.plist`, `PrivacyInfo.xcprivacy` | Copied from iOS; the `Uno.Sdk` picks both up from the tvOS folder. |

Not stamped:

- **No scene delegate / `UIApplicationSceneManifest`.** The tvOS head runs the app-level UIKit lifecycle, as the tvOS SamplesApp head in unoplatform/uno does.
- **No app icon or top shelf asset catalog.** `Uno.Resizetizer` treats tvOS as a generic Skia head and does not generate the layered `App Icon & Top Shelf Image` brand assets an App Store tvOS build needs. Apps that ship to the store add them by hand for now.

## Wizard

`TemplateWizard.json`:

- A new **TV** row in the Platforms screen holds the `tvos` card (`/Assets/Platforms.tvOS.svg`, provided by the Studio wizard), tagged **Skia**.
- `appId` also applies to `tvos` (it becomes the bundle identifier).
- `.NET MAUI Embedding` is disabled while tvOS is selected, since .NET MAUI does not support tvOS.

## Validation

- Two macOS CI scenarios: `-platforms tvos`, and `-preset recommended -platforms ios tvos`. tvOS heads only build on macOS (and Windows with the `tvos` workload); the `Uno.Sdk` drops the TFM on Linux.
