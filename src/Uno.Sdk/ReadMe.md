# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | 6.0.0-dev.39 |
| UnoExtensionsVersion | 5.3.0-dev.133 |
| UnoToolkitVersion | 6.5.0-dev.110 |
| UnoThemesVersion | 5.5.0-dev.95 |
| UnoCSharpMarkupVersion | 5.7.0-dev.12 |
| UnoWasmBootstrapVersion** | 8.0.23 |
| UnoLoggingVersion | 1.7.0 |
| UnoCoreLoggingSingletonVersion | 4.1.1 |
| UnoUniversalImageLoaderVersion | 1.9.37 |
| UnoDspTasksVersion | 1.4.0 |
| UnoResizetizerVersion | 1.8.0-dev.15 |
| SkiaSharpVersion | 3.119.0-preview.1.2 |
| SvgSkiaVersion | 2.0.0.4 |
| WinAppSdkVersion | 1.7.250310001 |
| WinAppSdkBuildToolsVersion | 10.0.26100.1742 |
| MicrosoftLoggingVersion** | 8.0.1 |
| WindowsCompatibilityVersion** | 8.0.14 |
| MicrosoftIdentityClientVersion | 4.70.0 |
| CommunityToolkitMvvmVersion | 8.4.0 |
| PrismVersion | 9.0.537 |
| AndroidMaterialVersion | 1.12.0.2 |
| AndroidXLegacySupportV4Version | 1.0.0.23 |
| AndroidXAppCompatVersion | 1.7.0.5 |
| AndroidXRecyclerViewVersion | 1.3.2.10 |
| AndroidXActivityVersion | 1.9.3.2 |
| AndroidXBrowserVersion | 1.8.0.8 |
| AndroidXSwipeRefreshLayoutVersion | 1.1.0.24 |
| AndroidXNavigationVersion | 2.8.5.1 |
| AndroidXCollectionVersion | 1.4.5.2 |
| MauiVersion** | 8.0.100 |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 9.0.

```json
[
  {
    "group": "Core",
    "version": "6.0.0-dev.39",
    "packages": [
      "Uno.WinUI",
      "Uno.UI.Adapter.Microsoft.Extensions.Logging",
      "Uno.WinUI.Maps",
      "Uno.WinUI.GooglePlay",
      "Uno.WinUI.Foldable",
      "Uno.WinUI.MSAL",
      "Uno.WinUI.Svg",
      "Uno.WinUI.Lottie",
      "Uno.WinUI.DevServer",
      "Uno.WinUI.Runtime.Skia.Linux.FrameBuffer",
      "Uno.WinUI.Runtime.Skia.MacOS",
      "Uno.WinUI.Runtime.Skia.Win32",
      "Uno.WinUI.Runtime.Skia.Wpf",
      "Uno.WinUI.Runtime.Skia.X11",
      "Uno.WinUI.Svg",
      "Uno.WinUI.WebAssembly",
      "Uno.WinUI.Runtime.Skia.Android",
      "Uno.WinUI.Runtime.Skia.AppleUIKit",
      "Uno.WinUI.Runtime.Skia.WebAssembly.Browser",
      "Uno.WinUI.Runtime.WebAssembly",
      "Uno.WinUI.MediaPlayer.WebAssembly",
      "Uno.WinUI.MediaPlayer.Skia.X11",
      "Uno.WinUI.MediaPlayer.Skia.Win32",
      "Uno.WinUI.Graphics3DGL",
      "Uno.WinUI.Graphics2DSK"
    ]
  },
  {
    "group": "WasmBootstrap",
    "version": "8.0.23",
    "packages": [
      "Uno.Wasm.Bootstrap",
      "Uno.Wasm.Bootstrap.DevServer",
      "Uno.Wasm.Bootstrap.Server"
    ],
    "versionOverride": {
      "net9.0": "9.0.14"
    }
  },
  {
    "group": "OSLogging",
    "version": "1.7.0",
    "packages": [
      "Uno.Extensions.Logging.OSLog",
      "Uno.Extensions.Logging.WebAssembly.Console"
    ]
  },
  {
    "group": "CoreLogging",
    "version": "4.1.1",
    "packages": [
      "Uno.Core.Extensions.Logging.Singleton"
    ]
  },
  {
    "group": "UniversalImageLoading",
    "version": "1.9.37",
    "packages": [
      "Uno.UniversalImageLoader"
    ]
  },
  {
    "group": "Dsp",
    "version": "1.4.0",
    "packages": [
      "Uno.Dsp.Tasks"
    ]
  },
  {
    "group": "Resizetizer",
    "version": "1.8.0-dev.15",
    "packages": [
      "Uno.Resizetizer"
    ]
  },
  {
    "group": "sdkextras",
    "version": "5.7.0-dev.4",
    "packages": [
      "Uno.Sdk.Extras"
    ]
  },
  {
    "group": "settings",
    "version": "1.3.0-dev.43",
    "packages": [
      "Uno.Settings.DevServer"
    ]
  },
  {
    "group": "hotdesign",
    "version": "1.10.0-dev.3",
    "packages": [
      "Uno.UI.HotDesign"
    ]
  },
  {
    "group": "SkiaSharp",
    "version": "3.119.0-preview.1.2",
    "packages": [
      "SkiaSharp.Skottie",
      "SkiaSharp.Views.Uno.WinUI",
      "SkiaSharp.Views.WinUI",
      "SkiaSharp.NativeAssets.Linux",
      "SkiaSharp.NativeAssets.macOS",
      "SkiaSharp.NativeAssets.Win32"
    ]
  },
  {
    "group": "SvgSkia",
    "version": "2.0.0.4",
    "packages": [
      "Svg.Skia"
    ]
  },
  {
    "group": "WinAppSdk",
    "version": "1.7.250310001",
    "packages": [
      "Microsoft.WindowsAppSDK"
    ]
  },
  {
    "group": "WinAppSdkBuildTools",
    "version": "10.0.26100.1742",
    "packages": [
      "Microsoft.Windows.SDK.BuildTools"
    ]
  },
  {
    "group": "MicrosoftLoggingConsole",
    "version": "8.0.1",
    "packages": [
      "Microsoft.Extensions.Logging.Console"
    ],
    "versionOverride": {
      "net9.0": "9.0.3"
    }
  },
  {
    "group": "WindowsCompatibility",
    "version": "8.0.14",
    "packages": [
      "Microsoft.Windows.Compatibility"
    ],
    "versionOverride": {
      "net9.0": "9.0.3"
    }
  },
  {
    "group": "MsalClient",
    "version": "4.70.0",
    "packages": [
      "Microsoft.Identity.Client",
      "Microsoft.Identity.Client.Extensions.Msal"
    ]
  },
  {
    "group": "Mvvm",
    "version": "8.4.0",
    "packages": [
      "CommunityToolkit.Mvvm"
    ]
  },
  {
    "group": "Prism",
    "version": "9.0.537",
    "packages": [
      "Prism.DryIoc.Uno.WinUI",
      "Prism.Uno.WinUI",
      "Prism.Uno.WinUI.Markup"
    ]
  },
  {
    "group": "UnoFonts",
    "version": "2.7.0-dev.5",
    "packages": [
      "Uno.Fonts.OpenSans",
      "Uno.Fonts.Fluent",
      "Uno.Fonts.Roboto"
    ]
  },
  {
    "group": "AndroidMaterial",
    "version": "1.12.0.2",
    "packages": [
      "Xamarin.Google.Android.Material"
    ]
  },
  {
    "group": "AndroidXLegacySupportV4",
    "version": "1.0.0.23",
    "packages": [
      "Xamarin.AndroidX.Legacy.Support.V4"
    ]
  },
  {
    "group": "AndroidXAppCompat",
    "version": "1.7.0.5",
    "packages": [
      "Xamarin.AndroidX.AppCompat"
    ]
  },
  {
    "group": "AndroidXRecyclerView",
    "version": "1.3.2.10",
    "packages": [
      "Xamarin.AndroidX.RecyclerView"
    ]
  },
  {
    "group": "AndroidXActivity",
    "version": "1.9.3.2",
    "packages": [
      "Xamarin.AndroidX.Activity"
    ]
  },
  {
    "group": "AndroidXBrowser",
    "version": "1.8.0.8",
    "packages": [
      "Xamarin.AndroidX.Browser"
    ]
  },
  {
    "group": "AndroidXSwipeRefreshLayout",
    "version": "1.1.0.24",
    "packages": [
      "Xamarin.AndroidX.SwipeRefreshLayout"
    ]
  },
  {
    "group": "AndroidXNavigation",
    "version": "2.8.5.1",
    "packages": [
      "Xamarin.AndroidX.Navigation.UI",
      "Xamarin.AndroidX.Navigation.Fragment",
      "Xamarin.AndroidX.Navigation.Runtime",
      "Xamarin.AndroidX.Navigation.Common"
    ]
  },
  {
    "group": "AndroidXCollection",
    "version": "1.4.5.2",
    "packages": [
      "Xamarin.AndroidX.Collection",
      "Xamarin.AndroidX.Collection.Ktx"
    ]
  },
  {
    "group": "Maui",
    "version": "8.0.100",
    "packages": [
      "Microsoft.Maui.Controls",
      "Microsoft.Maui.Controls.Compatibility",
      "Microsoft.Maui.Graphics"
    ],
    "versionOverride": {
      "net9.0": "9.0.50"
    }
  },
  {
    "group": "CSharpMarkup",
    "version": "5.7.0-dev.12",
    "packages": [
      "Uno.WinUI.Markup",
      "Uno.Extensions.Markup.Generators"
    ]
  },
  {
    "group": "Extensions",
    "version": "5.3.0-dev.133",
    "packages": [
      "Uno.Extensions.Authentication.WinUI",
      "Uno.Extensions.Authentication.MSAL.WinUI",
      "Uno.Extensions.Authentication.Oidc.WinUI",
      "Uno.Extensions.Configuration",
      "Uno.Extensions.Core.WinUI",
      "Uno.Extensions.Hosting.WinUI",
      "Uno.Extensions.Http.WinUI",
      "Uno.Extensions.Http.Refit",
      "Uno.Extensions.Localization.WinUI",
      "Uno.Extensions.Logging.WinUI",
      "Uno.Extensions.Maui.WinUI",
      "Uno.Extensions.Maui.WinUI.Markup",
      "Uno.Extensions.Navigation.WinUI",
      "Uno.Extensions.Navigation.WinUI.Markup",
      "Uno.Extensions.Navigation.Toolkit.WinUI",
      "Uno.Extensions.Reactive.WinUI",
      "Uno.Extensions.Reactive.Messaging",
      "Uno.Extensions.Reactive.WinUI.Markup",
      "Uno.Extensions.Serialization.Http",
      "Uno.Extensions.Serialization.Refit",
      "Uno.Extensions.Logging.Serilog",
      "Uno.Extensions.Storage.WinUI"
    ]
  },
  {
    "group": "Toolkit",
    "version": "6.5.0-dev.110",
    "packages": [
      "Uno.Toolkit.WinUI",
      "Uno.Toolkit.WinUI.Cupertino",
      "Uno.Toolkit.WinUI.Material",
      "Uno.Toolkit.WinUI.Material.Markup",
      "Uno.Toolkit.WinUI.Markup",
      "Uno.Toolkit.Skia.WinUI"
    ]
  },
  {
    "group": "Themes",
    "version": "5.5.0-dev.95",
    "packages": [
      "Uno.Material.WinUI",
      "Uno.Material.WinUI.Markup",
      "Uno.Themes.WinUI.Markup",
      "Uno.Cupertino.WinUI"
    ]
  }
]
```
