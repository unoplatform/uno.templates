# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | 5.5.0-dev.562 |
| UnoExtensionsVersion | 5.1.0-dev.60 |
| UnoToolkitVersion | 6.3.0-dev.6 |
| UnoThemesVersion | 5.3.0-dev.8 |
| UnoCSharpMarkupVersion | 5.4.3 |
| UnoWasmBootstrapVersion** | 8.0.21 |
| UnoLoggingVersion | 1.7.0 |
| UnoCoreLoggingSingletonVersion | 4.1.1 |
| UnoUniversalImageLoaderVersion | 1.9.37 |
| UnoDspTasksVersion | 1.4.0-dev.12 |
| UnoResizetizerVersion | 1.6.0-dev.31 |
| SkiaSharpVersion | 2.88.8 |
| SvgSkiaVersion | 2.0.0.1 |
| WinAppSdkVersion | 1.6.240829007 |
| WinAppSdkBuildToolsVersion | 10.0.26100.1742 |
| MicrosoftLoggingVersion** | 8.0.0 |
| WindowsCompatibilityVersion** | 8.0.8 |
| MicrosoftIdentityClientVersion | 4.65.0 |
| CommunityToolkitMvvmVersion | 8.3.2 |
| PrismVersion | 9.0.537 |
| AndroidMaterialVersion | 1.11.0.2 |
| AndroidXLegacySupportV4Version | 1.0.0.23 |
| AndroidXAppCompatVersion | 1.7.0.2 |
| AndroidXRecyclerViewVersion | 1.3.2.7 |
| AndroidXActivityVersion | 1.9.1.1 |
| AndroidXBrowserVersion | 1.8.0.4 |
| AndroidXSwipeRefreshLayoutVersion | 1.1.0.22 |
| AndroidXNavigationVersion | 2.7.7.6 |
| AndroidXCollectionVersion | 1.4.2.1 |
| MauiVersion** | 8.0.91 |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 9.0.

```json
[
  {
    "group": "Core",
    "version": "5.5.0-dev.562",
    "packages": [
      "Uno.WinUI",
      "Uno.UI.Adapter.Microsoft.Extensions.Logging",
      "Uno.WinUI.Maps",
      "Uno.WinUI.Foldable",
      "Uno.WinUI.MSAL",
      "Uno.WinUI.Svg",
      "Uno.WinUI.Lottie",
      "Uno.WinUI.DevServer",
      "Uno.WinUI.Skia.Gtk",
      "Uno.WinUI.Skia.Linux.FrameBuffer",
      "Uno.WinUI.Skia.MacOS",
      "Uno.WinUI.Skia.Wpf",
      "Uno.WinUI.Skia.X11",
      "Uno.WinUI.Svg",
      "Uno.WinUI.WebAssembly",
      "Uno.WinUI.Runtime.WebAssembly",
      "Uno.WinUI.MediaPlayer.WebAssembly",
      "Uno.WinUI.Graphics3DGL",
      "Uno.WinUI.Graphics2DSK"
    ]
  },
  {
    "group": "WasmBootstrap",
    "version": "8.0.21",
    "packages": [
      "Uno.Wasm.Bootstrap",
      "Uno.Wasm.Bootstrap.DevServer",
      "Uno.Wasm.Bootstrap.Server"
    ],
    "versionOverride": {
      "net9.0": "9.0.0-dev.309"
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
    "version": "1.4.0-dev.12",
    "packages": [
      "Uno.Dsp.Tasks"
    ]
  },
  {
    "group": "Resizetizer",
    "version": "1.6.0-dev.31",
    "packages": [
      "Uno.Resizetizer"
    ]
  },
  {
    "group": "SkiaSharp",
    "version": "2.88.8",
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
    "version": "2.0.0.1",
    "packages": [
      "Svg.Skia"
    ]
  },
  {
    "group": "WinAppSdk",
    "version": "1.6.240829007",
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
    "version": "8.0.0",
    "packages": [
      "Microsoft.Extensions.Logging.Console"
    ],
    "versionOverride": {
      "net9.0": "9.0.0-rc.1.24431.7"
    }
  },
  {
    "group": "WindowsCompatibility",
    "version": "8.0.8",
    "packages": [
      "Microsoft.Windows.Compatibility"
    ],
    "versionOverride": {
      "net9.0": "9.0.0-rc.1.24452.1"
    }
  },
  {
    "group": "MsalClient",
    "version": "4.65.0",
    "packages": [
      "Microsoft.Identity.Client",
      "Microsoft.Identity.Client.Extensions.Msal"
    ]
  },
  {
    "group": "Mvvm",
    "version": "8.3.2",
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
    "version": "2.5.0-dev.9",
    "packages": [
      "Uno.Fonts.OpenSans",
      "Uno.Fonts.Fluent",
      "Uno.Fonts.Roboto"
    ]
  },
  {
    "group": "AndroidMaterial",
    "version": "1.11.0.2",
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
    "version": "1.7.0.2",
    "packages": [
      "Xamarin.AndroidX.AppCompat"
    ]
  },
  {
    "group": "AndroidXRecyclerView",
    "version": "1.3.2.7",
    "packages": [
      "Xamarin.AndroidX.RecyclerView"
    ]
  },
  {
    "group": "AndroidXActivity",
    "version": "1.9.1.1",
    "packages": [
      "Xamarin.AndroidX.Activity"
    ]
  },
  {
    "group": "AndroidXBrowser",
    "version": "1.8.0.4",
    "packages": [
      "Xamarin.AndroidX.Browser"
    ]
  },
  {
    "group": "AndroidXSwipeRefreshLayout",
    "version": "1.1.0.22",
    "packages": [
      "Xamarin.AndroidX.SwipeRefreshLayout"
    ]
  },
  {
    "group": "AndroidXNavigation",
    "version": "2.7.7.6",
    "packages": [
      "Xamarin.AndroidX.Navigation.UI",
      "Xamarin.AndroidX.Navigation.Fragment",
      "Xamarin.AndroidX.Navigation.Runtime",
      "Xamarin.AndroidX.Navigation.Common"
    ]
  },
  {
    "group": "AndroidXCollection",
    "version": "1.4.2.1",
    "packages": [
      "Xamarin.AndroidX.Collection",
      "Xamarin.AndroidX.Collection.Ktx"
    ]
  },
  {
    "group": "Maui",
    "version": "8.0.91",
    "packages": [
      "Microsoft.Maui.Controls",
      "Microsoft.Maui.Controls.Compatibility",
      "Microsoft.Maui.Graphics"
    ],
    "versionOverride": {
      "net9.0": "9.0.0-rc.1.24453.9"
    }
  },
  {
    "group": "CSharpMarkup",
    "version": "5.4.3",
    "packages": [
      "Uno.WinUI.Markup",
      "Uno.Extensions.Markup.Generators"
    ]
  },
  {
    "group": "Extensions",
    "version": "5.1.0-dev.60",
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
    "version": "6.3.0-dev.6",
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
    "version": "5.3.0-dev.8",
    "packages": [
      "Uno.Material.WinUI",
      "Uno.Material.WinUI.Markup",
      "Uno.Themes.WinUI.Markup",
      "Uno.Cupertino.WinUI"
    ]
  }
]
```
