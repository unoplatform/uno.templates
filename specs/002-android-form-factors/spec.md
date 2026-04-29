# Spec: Android TV, Android Auto, and Wear OS opt-ins

## Summary

Add three new opt-in checkboxes to the `unoapp` template — **Android TV**, **Android Auto**, and **Wear OS** — that surface in the Studio wizard whenever the **Android** platform is selected. Selecting any of them stamps the corresponding manifest entries, intent filters, resources, and `<UnoFeatures>` flags into the generated project so the user does not need to perform any manual Android setup to target these form factors.

## Motivation

Today, enabling Android TV requires a multi-step manual procedure (intent filter, banner drawable, `[UsesFeature]` attributes, focus highlight overrides) documented at <xref:Uno.Features.AndroidTv>. Android Auto and Wear OS are not supported as opt-ins at all — users have to research the manifest declarations and AndroidX packages on their own. This friction discourages developers from extending their Uno apps to these form factors.

The new flags collapse all of that into a single checkbox and a single `<UnoFeatures>` entry that the `Uno.Sdk` recognizes (see the corresponding plumbing in `unoplatform/uno`).

## Scope

These flags configure a **single APK** that targets phones plus the chosen form factor(s). The conventional Wear-only standalone APK pattern — where a Wear app is its own dedicated project — remains a manual setup; the `AndroidWear` opt-in covers the dual-form-factor pattern only.

## Template parameters

| Symbol | Type | Default | Visibility |
|---|---|---|---|
| `includeAndroidTV` | `bool` parameter | `false` | Wizard checkbox shown when `platforms` includes `android` |
| `includeAndroidAuto` | `bool` parameter | `false` | Wizard checkbox shown when `platforms` includes `android` |
| `includeAndroidWear` | `bool` parameter | `false` | Wizard checkbox shown when `platforms` includes `android` |

The wizard renders these in a new **Android form factors** sub-section inside the existing **Platforms** screen.

## What gets stamped

### `Platforms/Android/AndroidManifest.xml`

Conditional `<uses-feature>` entries are added to the root `<manifest>` element:

```xml
<!--#if (includeAndroidTV)-->
<uses-feature android:name="android.software.leanback" android:required="false" />
<uses-feature android:name="android.hardware.touchscreen" android:required="false" />
<!--#endif-->
<!--#if (includeAndroidWear)-->
<uses-feature android:name="android.hardware.type.watch" android:required="false" />
<!--#endif-->
```

A `<meta-data>` element is added inside `<application>` for Auto:

```xml
<!--#if (includeAndroidAuto)-->
<meta-data android:name="com.google.android.gms.car.application"
           android:resource="@xml/automotive_app_desc" />
<!--#endif-->
```

### `Platforms/Android/MainActivity.Android.cs`

A leanback `IntentFilter` is appended on the activity when `includeAndroidTV` is true.

### `Platforms/Android/Main.Android.cs`

Two assembly-level `[UsesFeature]` attributes (leanback + touchscreen, both `Required = false`) are added when `includeAndroidTV` is true, plus `Banner = "@drawable/banner"` on the `[ApplicationAttribute]`.

### `MyExtensionsApp.1.csproj`

Conditional `<UnoFeatures>` entries (`AndroidTV`, `AndroidAuto`, `AndroidWear`) are appended so the `Uno.Sdk` resolves the correct AndroidX packages (Leanback / Car.App / Wear + Wear.Tiles).

### Resources

| File | Included when |
|---|---|
| `Resources/drawable-xhdpi/banner.png` (320×180 placeholder) | `includeAndroidTV` |
| `Resources/values/Styles.AndroidTV.xml` (transparent control highlight) | `includeAndroidTV` |
| `Resources/xml/automotive_app_desc.xml` | `includeAndroidAuto` |

The placeholder banner is intentionally generic — users are expected to replace it with their own 320×180 banner image (and provide localized variants under `drawable-xhdpi-{lang}` if needed) before publishing.

## Out of scope

- A standalone Wear-only project template variant. Users who want a Wear-only APK should create a second Uno Platform project dedicated to Wear and mark `android.hardware.type.watch` as `Required = true`.
- Generation of a `CarAppService` or media browser scaffold. The `AndroidAuto` opt-in only declares the manifest plumbing; users provide the service implementation themselves.
- Platform abstraction inside `Uno.UI` for runtime form-factor detection.

## Validation

1. `dotnet new install` the locally-built `Uno.Templates` package.
2. Run `dotnet new unoapp -o TVApp -platforms android --include-android-tv true` (and similar for Auto / Wear).
3. Verify the generated `AndroidManifest.xml`, `MainActivity.Android.cs`, `Main.Android.cs`, and `csproj` contain the expected stamped entries.
4. `dotnet build TVApp/TVApp -f net10.0-android` should succeed with the AndroidX packages resolved.
5. Smoke-deploy to the appropriate emulator (Android TV, Android Auto via DHU, Wear OS) and verify the app launches and surfaces correctly.
