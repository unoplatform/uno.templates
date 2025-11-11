# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | 6.4.43 |
| UnoExtensionsVersion | 7.0.4 |
| UnoToolkitVersion | 8.3.2 |
| UnoThemesVersion | 6.0.2 |
| UnoCSharpMarkupVersion | 6.4.11 |
| UnoWasmBootstrapVersion** | 9.0.20 |
| UnoLoggingVersion | 1.7.0 |
| UnoCoreLoggingSingletonVersion | 4.1.1 |
| UnoUniversalImageLoaderVersion | 1.9.37 |
| UnoDspTasksVersion | 1.4.0 |
| UnoResizetizerVersion | 1.12.1 |
| SkiaSharpVersion | 3.119.1 |
| SvgSkiaVersion | 3.0.6 |
| WinAppSdkVersion | 1.7.250909003 |
| WinAppSdkBuildToolsVersion | 10.0.26100.6901 |
| MicrosoftLoggingVersion** | 9.0.10 |
| WindowsCompatibilityVersion** | 9.0.10 |
| MicrosoftIdentityClientVersion | 4.78.0 |
| CommunityToolkitMvvmVersion | 8.4.0 |
| PrismVersion | 9.0.537 |
| AndroidMaterialVersion | 1.12.0.4 |
| AndroidXLegacySupportV4Version | 1.0.0.23 |
| AndroidXSplashScreenVersion | 1.0.1.14 |
| AndroidXAppCompatVersion | 1.7.0.7 |
| AndroidXRecyclerViewVersion | 1.4.0.2 |
| AndroidXActivityVersion | 1.10.1.2 |
| AndroidXBrowserVersion | 1.8.0.10 |
| AndroidXSwipeRefreshLayoutVersion | 1.1.0.28 |
| AndroidXNavigationVersion | 2.8.9.2 |
| AndroidXCollectionVersion | 1.5.0.2 |
| MauiVersion** | 9.0.120 |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 10.0.

```json
[
  {
    "group": "Core",
    "version": "6.4.43",
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
      "Uno.WinUI.WebView.Skia.X11",
      "Uno.WinUI.Graphics3DGL",
      "Uno.WinUI.Graphics2DSK"
    ]
  },
  {
    "group": "WasmBootstrap",
    "version": "9.0.20",
    "packages": [
      "Uno.Wasm.Bootstrap",
      "Uno.Wasm.Bootstrap.DevServer",
      "Uno.Wasm.Bootstrap.Server"
    ],
    "versionOverride": {
      "net10.0": "10.0.1"
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
    "version": "1.12.1",
    "packages": [
      "Uno.Resizetizer"
    ]
  },
  {
    "group": "sdkextras",
    "version": "6.2.0-dev.4",
    "packages": [
      "Uno.Sdk.Extras"
    ]
  },
  {
    "group": "settings",
    "version": "1.7.1",
    "packages": [
      "Uno.Settings.DevServer"
    ]
  },
  {
    "group": "hotdesign",
    "version": "1.17.112",
    "packages": [
      "Uno.UI.HotDesign"
    ]
  },
  {
    "group": "SkiaSharp",
    "version": "3.119.1",
    "packages": [
      "SkiaSharp.Skottie",
      "SkiaSharp.Views.Uno.WinUI",
      "SkiaSharp.Views.WinUI",
      "SkiaSharp.NativeAssets.Linux",
      "SkiaSharp.NativeAssets.macOS",
      "SkiaSharp.NativeAssets.Win32",
      "SkiaSharp.NativeAssets.WebAssembly"
    ]
  },
  {
    "group": "SvgSkia",
    "version": "3.0.6",
    "packages": [
      "Svg.Skia"
    ]
  },
  {
    "group": "WinAppSdk",
    "version": "1.7.250909003",
    "packages": [
      "Microsoft.WindowsAppSDK"
    ]
  },
  {
    "group": "WinAppSdkBuildTools",
    "version": "10.0.26100.6901",
    "packages": [
      "Microsoft.Windows.SDK.BuildTools"
    ]
  },
  {
    "group": "MicrosoftLoggingConsole",
    "version": "9.0.10",
    "packages": [
      "Microsoft.Extensions.Logging.Console"
    ],
    "versionOverride": {
      "net10.0": "10.0.0-rc.2.25502.107"
    }
  },
  {
    "group": "WindowsCompatibility",
    "version": "9.0.10",
    "packages": [
      "Microsoft.Windows.Compatibility"
    ],
    "versionOverride": {
      "net10.0": "10.0.0-rc.2.25502.107"
    }
  },
  {
    "group": "MsalClient",
    "version": "4.78.0",
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
    "version": "2.8.1",
    "packages": [
      "Uno.Fonts.OpenSans",
      "Uno.Fonts.Fluent",
      "Uno.Fonts.Roboto"
    ]
  },
  {
    "group": "AndroidMaterial",
    "version": "1.12.0.4",
    "packages": [
      "Xamarin.Google.Android.Material"
    ],
    "versionOverride": {
      "net10.0": "1.12.0.5"
    }
  },
  {
    "group": "AndroidXLegacySupportV4",
    "version": "1.0.0.23",
    "packages": [
      "Xamarin.AndroidX.Legacy.Support.V4"
    ],
    "versionOverride": {
      "net10.0": "1.0.0.33"
    }
  },
  {
    "group": "AndroidXSplashScreen",
    "version": "1.0.1.14",
    "packages": [
      "Xamarin.AndroidX.Core.SplashScreen"
    ],
    "versionOverride": {
      "net10.0": "1.0.1.14"
    }
  },
  {
    "group": "AndroidXAppCompat",
    "version": "1.7.0.7",
    "packages": [
      "Xamarin.AndroidX.AppCompat"
    ],
    "versionOverride": {
      "net10.0": "1.7.1.1"
    }
  },
  {
    "group": "AndroidXRecyclerView",
    "version": "1.4.0.2",
    "packages": [
      "Xamarin.AndroidX.RecyclerView"
    ],
    "versionOverride": {
      "net10.0": "1.4.0.3"
    }
  },
  {
    "group": "AndroidXActivity",
    "version": "1.10.1.2",
    "packages": [
      "Xamarin.AndroidX.Activity"
    ],
    "versionOverride": {
      "net10.0": "1.10.1.3"
    }
  },
  {
    "group": "AndroidXBrowser",
    "version": "1.8.0.10",
    "packages": [
      "Xamarin.AndroidX.Browser"
    ],
    "versionOverride": {
      "net10.0": "1.8.0.11"
    }
  },
  {
    "group": "AndroidXSwipeRefreshLayout",
    "version": "1.1.0.28",
    "packages": [
      "Xamarin.AndroidX.SwipeRefreshLayout"
    ],
    "versionOverride": {
      "net10.0": "1.1.0.29"
    }
  },
  {
    "group": "AndroidXNavigation",
    "version": "2.8.9.2",
    "packages": [
      "Xamarin.AndroidX.Navigation.UI",
      "Xamarin.AndroidX.Navigation.Fragment",
      "Xamarin.AndroidX.Navigation.Runtime",
      "Xamarin.AndroidX.Navigation.Common"
    ],
    "versionOverride": {
      "net10.0": "2.9.2.1"
    }
  },
  {
    "group": "AndroidXCollection",
    "version": "1.5.0.2",
    "packages": [
      "Xamarin.AndroidX.Collection",
      "Xamarin.AndroidX.Collection.Ktx"
    ],
    "versionOverride": {
      "net10.0": "1.5.0.3"
    }
  },
  {
    "group": "Maui",
    "version": "9.0.120",
    "packages": [
      "Microsoft.Maui.Controls",
      "Microsoft.Maui.Controls.Compatibility",
      "Microsoft.Maui.Graphics"
    ],
    "versionOverride": {
      "net10.0": "10.0.0-rc.2.25504.7"
    }
  },
  {
    "group": "CSharpMarkup",
    "version": "6.4.11",
    "packages": [
      "Uno.WinUI.Markup",
      "Uno.Extensions.Markup.Generators"
    ]
  },
  {
    "group": "Extensions",
    "version": "7.0.4",
    "packages": [
      "Uno.Extensions.Authentication.WinUI",
      "Uno.Extensions.Authentication.MSAL.WinUI",
      "Uno.Extensions.Authentication.Oidc.WinUI",
      "Uno.Extensions.Configuration",
      "Uno.Extensions.Core.WinUI",
      "Uno.Extensions.Hosting.WinUI",
      "Uno.Extensions.Http.WinUI",
      "Uno.Extensions.Http.Refit",
      "Uno.Extensions.Http.Kiota",
      "Uno.Extensions.Localization.WinUI",
      "Uno.Extensions.Logging.WinUI",
      "Uno.Extensions.Maui.WinUI",
      "Uno.Extensions.Maui.WinUI.Markup",
      "Uno.Extensions.Maui.WinUI.Runtime.Skia",
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
    "version": "8.3.2",
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
    "version": "6.0.2",
    "packages": [
      "Uno.Material.WinUI",
      "Uno.Material.WinUI.Markup",
      "Uno.Themes.WinUI.Markup",
      "Uno.Cupertino.WinUI"
    ]
  },
  {
    "group": "VlcNativeWindowsAssets",
    "version": "3.0.21",
    "packages": [
      "VideoLAN.LibVLC.Windows"
    ]
  },
  {
    "group": "MicrosoftWebView2",
    "version": "1.0.3595.46",
    "packages": [
      "Microsoft.Web.WebView2"
    ]
  },
  {
    "group": "AppMcp",
    "version": "1.0.6",
    "packages": [
      "Uno.UI.App.Mcp"
    ]
  }
]
```
