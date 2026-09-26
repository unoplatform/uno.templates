# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | 7.0.0-dev.1215 |
| UnoExtensionsVersion | 8.0.0-dev.55 |
| UnoToolkitVersion | 11.0.0-dev.94 |
| UnoThemesVersion | 9.0.0-dev.26 |
| UnoCSharpMarkupVersion | 7.0.0-dev.33 |
| UnoWasmBootstrapVersion** | 10.0.98 |
| UnoLoggingVersion | 1.7.0 |
| UnoCoreLoggingSingletonVersion | 5.0.0-dev.28 |
| UnoUniversalImageLoaderVersion | 1.9.37 |
| UnoDspTasksVersion | 1.4.0 |
| UnoResizetizerVersion | 2.0.0-dev.2 |
| SkiaSharpVersion | 4.151.1 |
| SvgSkiaVersion | 3.0.6 |
| WinAppSdkVersion | 2.4.0 |
| WinAppSdkBuildToolsVersion | 10.0.28000.2705 |
| WinAppSdkBuildToolsWinAppVersion | 0.7.0 |
| MicrosoftLoggingVersion** | 10.0.12 |
| WindowsCompatibilityVersion** | 9.0.20 |
| MicrosoftIdentityClientVersion | 4.90.1 |
| CommunityToolkitMvvmVersion | 8.4.2 |
| PrismVersion | 9.0.537 |
| AndroidMaterialVersion | 1.14.0.6 |
| AndroidXLegacySupportV4Version | 1.0.0.23 |
| AndroidXSplashScreenVersion | 1.2.0.3 |
| AndroidXAppCompatVersion | 1.8.0 |
| AndroidXRecyclerViewVersion | 1.4.0.6 |
| AndroidXActivityVersion | 1.13.0.1 |
| AndroidXBrowserVersion | 1.10.0.1 |
| AndroidXSwipeRefreshLayoutVersion | 1.2.0.3 |
| AndroidXLeanbackVersion | 1.2.0.4 |
| AndroidXCarAppVersion | 1.7.0.4 |
| AndroidXWearVersion | 1.4.0.2 |
| AndroidXWearTilesVersion | 1.6.1 |
| AndroidXNavigationVersion | 2.9.8.1 |
| AndroidXCollectionVersion | 1.6.0.1 |
| MauiVersion** | 10.0.110 |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 10.0.

```json
[
  {
    "group": "Core",
    "version": "7.0.0-dev.1215",
    "packages": [
      "Uno.WinUI",
      "Uno.WinUI.Composition.Skia",
      "Uno.WinUI.Composition.WebGpu",
      "Uno.UI.Adapter.Microsoft.Extensions.Logging",
      "Uno.WinUI.GooglePlay",
      "Uno.WinUI.Foldable",
      "Uno.WinUI.MSAL",
      "Uno.WinUI.Svg",
      "Uno.WinUI.Lottie",
      "Uno.WinUI.DevServer",
      "Uno.WinUI.Runtime.Skia.Linux.FrameBuffer",
      "Uno.WinUI.Runtime.Skia.MacOS",
      "Uno.WinUI.Runtime.Skia.Win32",
      "Uno.WinUI.Runtime.Skia.X11",
      "Uno.WinUI.Runtime.Skia.Android",
      "Uno.WinUI.Runtime.Skia.AppleUIKit",
      "Uno.WinUI.Runtime.Skia.WebAssembly.Browser",
      "Uno.WinUI.MediaPlayer.Skia.X11",
      "Uno.WinUI.MediaPlayer.Skia.Win32",
      "Uno.WinUI.WebView.Skia.X11",
      "Uno.WinUI.Graphics3DGL",
      "Uno.WinUI.Graphics2DSK",
      "Uno.WinUI.SpellChecking"
    ]
  },
  {
    "group": "WasmBootstrap",
    "version": "10.0.98",
    "packages": [
      "Uno.Wasm.Bootstrap",
      "Uno.Wasm.Bootstrap.DevServer",
      "Uno.Wasm.Bootstrap.Server"
    ],
    "versionOverride": {
      "net10.0": "10.1.0-dev.217",
      "net11.0": "10.1.0-dev.217"
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
    "version": "5.0.0-dev.28",
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
    "version": "2.0.0-dev.2",
    "packages": [
      "Uno.Resizetizer"
    ]
  },
  {
    "group": "sdkextras",
    "version": "6.4.0-dev.13",
    "packages": [
      "Uno.Sdk.Extras"
    ]
  },
  {
    "group": "settings",
    "version": "2.0.0-dev.10",
    "packages": [
      "Uno.Settings.DevServer"
    ]
  },
  {
    "group": "hotdesign",
    "version": "1.23.0-dev.251",
    "packages": [
      "Uno.UI.HotDesign"
    ]
  },
  {
    "group": "SkiaSharp",
    "version": "4.151.1",
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
    "version": "2.4.0",
    "packages": [
      "Microsoft.WindowsAppSDK"
    ]
  },
  {
    "group": "WinAppSdkBuildTools",
    "version": "10.0.28000.2705",
    "packages": [
      "Microsoft.Windows.SDK.BuildTools"
    ]
  },
  {
    "group": "WinAppSdkBuildToolsWinApp",
    "version": "0.7.0",
    "packages": [
      "Microsoft.Windows.SDK.BuildTools.WinApp"
    ]
  },
  {
    "group": "MicrosoftLoggingConsole",
    "version": "10.0.12",
    "packages": [
      "Microsoft.Extensions.Logging.Console"
    ],
    "versionOverride": {
      "net10.0": "10.0.12",
      "net11.0": "11.0.0-rc.1.26425.128"
    }
  },
  {
    "group": "WindowsCompatibility",
    "version": "9.0.20",
    "packages": [
      "Microsoft.Windows.Compatibility"
    ],
    "versionOverride": {
      "net10.0": "10.0.12"
    }
  },
  {
    "group": "MsalClient",
    "version": "4.90.1",
    "packages": [
      "Microsoft.Identity.Client",
      "Microsoft.Identity.Client.Extensions.Msal"
    ]
  },
  {
    "group": "Mvvm",
    "version": "8.4.2",
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
    "version": "2.10.0-dev.9",
    "packages": [
      "Uno.Fonts.OpenSans",
      "Uno.Fonts.Fluent",
      "Uno.Fonts.Roboto"
    ]
  },
  {
    "group": "AndroidMaterial",
    "version": "1.14.0.6",
    "packages": [
      "Xamarin.Google.Android.Material"
    ],
    "versionOverride": {
      "net10.0": "1.14.0.6"
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
    "version": "1.2.0.3",
    "packages": [
      "Xamarin.AndroidX.Core.SplashScreen"
    ],
    "versionOverride": {
      "net10.0": "1.2.0.3"
    }
  },
  {
    "group": "AndroidXAppCompat",
    "version": "1.8.0",
    "packages": [
      "Xamarin.AndroidX.AppCompat"
    ],
    "versionOverride": {
      "net10.0": "1.8.0"
    }
  },
  {
    "group": "AndroidXRecyclerView",
    "version": "1.4.0.6",
    "packages": [
      "Xamarin.AndroidX.RecyclerView"
    ],
    "versionOverride": {
      "net10.0": "1.4.0.6"
    }
  },
  {
    "group": "AndroidXActivity",
    "version": "1.13.0.1",
    "packages": [
      "Xamarin.AndroidX.Activity"
    ],
    "versionOverride": {
      "net10.0": "1.13.0.1"
    }
  },
  {
    "group": "AndroidXBrowser",
    "version": "1.10.0.1",
    "packages": [
      "Xamarin.AndroidX.Browser"
    ],
    "versionOverride": {
      "net10.0": "1.10.0.1"
    }
  },
  {
    "group": "AndroidXSwipeRefreshLayout",
    "version": "1.2.0.3",
    "packages": [
      "Xamarin.AndroidX.SwipeRefreshLayout"
    ],
    "versionOverride": {
      "net10.0": "1.2.0.3"
    }
  },
  {
    "group": "AndroidXLeanback",
    "version": "1.2.0.4",
    "packages": [
      "Xamarin.AndroidX.Leanback"
    ],
    "versionOverride": {
      "net10.0": "1.2.0.4"
    }
  },
  {
    "group": "AndroidXCarApp",
    "version": "1.7.0.4",
    "packages": [
      "Xamarin.AndroidX.Car.App.App"
    ],
    "versionOverride": {
      "net10.0": "1.7.0.4"
    }
  },
  {
    "group": "AndroidXWear",
    "version": "1.4.0.2",
    "packages": [
      "Xamarin.AndroidX.Wear"
    ],
    "versionOverride": {
      "net10.0": "1.4.0.2"
    }
  },
  {
    "group": "AndroidXWearTiles",
    "version": "1.6.1",
    "packages": [
      "Xamarin.AndroidX.Wear.Tiles"
    ],
    "versionOverride": {
      "net10.0": "1.6.1"
    }
  },
  {
    "group": "AndroidXNavigation",
    "version": "2.9.8.1",
    "packages": [
      "Xamarin.AndroidX.Navigation.UI",
      "Xamarin.AndroidX.Navigation.Fragment",
      "Xamarin.AndroidX.Navigation.Runtime",
      "Xamarin.AndroidX.Navigation.Common"
    ],
    "versionOverride": {
      "net10.0": "2.9.8.1"
    }
  },
  {
    "group": "AndroidXCollection",
    "version": "1.6.0.1",
    "packages": [
      "Xamarin.AndroidX.Collection",
      "Xamarin.AndroidX.Collection.Ktx"
    ],
    "versionOverride": {
      "net10.0": "1.6.0.1"
    }
  },
  {
    "group": "Maui",
    "version": "10.0.110",
    "packages": [
      "Microsoft.Maui.Controls",
      "Microsoft.Maui.Graphics"
    ],
    "versionOverride": {
      "net10.0": "10.0.110",
      "net11.0": "11.0.0-rc.1.26451.6"
    }
  },
  {
    "group": "CSharpMarkup",
    "version": "7.0.0-dev.33",
    "packages": [
      "Uno.WinUI.Markup",
      "Uno.Extensions.Markup.Generators"
    ]
  },
  {
    "group": "Extensions",
    "version": "8.0.0-dev.55",
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
    "version": "11.0.0-dev.94",
    "packages": [
      "Uno.Toolkit.WinUI",
      "Uno.Toolkit.WinUI.Cupertino",
      "Uno.Toolkit.WinUI.Material",
      "Uno.Toolkit.WinUI.Material.Markup",
      "Uno.Toolkit.WinUI.Markup",
      "Uno.Toolkit.Skia.WinUI",
      "Uno.Toolkit.WinUI.Simple"
    ]
  },
  {
    "group": "Themes",
    "version": "9.0.0-dev.26",
    "packages": [
      "Uno.Material.WinUI",
      "Uno.Material.WinUI.Markup",
      "Uno.Themes.WinUI.Markup",
      "Uno.Cupertino.WinUI",
      "Uno.Simple.WinUI",
      "Uno.Simple.WinUI.Markup"
    ]
  },
  {
    "group": "VlcNativeWindowsAssets",
    "version": "3.0.24",
    "packages": [
      "VideoLAN.LibVLC.Windows"
    ]
  },
  {
    "group": "MicrosoftWebView2",
    "version": "1.0.4191.47",
    "packages": [
      "Microsoft.Web.WebView2"
    ]
  },
  {
    "group": "AppMcp",
    "version": "2.0.0-dev.4",
    "packages": [
      "Uno.UI.App.Mcp"
    ]
  },
  {
    "group": "MauiCompatibility",
    "version": "10.0.110",
    "packages": [
      "Microsoft.Maui.Controls.Compatibility"
    ]
  }
]
```
