# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | $Core$ |
| UnoExtensionsVersion | $Extensions$ |
| UnoToolkitVersion | $Toolkit$ |
| UnoThemesVersion | $Themes$ |
| UnoCSharpMarkupVersion | $CSharpMarkup$ |
| UnoReactorVersion | $Reactor$ |
| UnoMauiVersion | $UnoMaui$ |
| UnoRetargetVersion | $UnoRetarget$ |
| UnoWasmBootstrapVersion** | $WasmBootstrap$ |
| UnoLoggingVersion | $OSLogging$ |
| UnoCoreLoggingSingletonVersion | $CoreLogging$ |
| UnoDspTasksVersion | $Dsp$ |
| UnoResizetizerVersion | $Resizetizer$ |
| UnoFontsVersion | $UnoFonts$ |
| UnoSdkExtrasVersion | $sdkextras$ |
| UnoSettingsVersion | $settings$ |
| UnoHotDesignVersion | $hotdesign$ |
| UnoHotTestingVersion | $hottesting$ |
| UnoAppMcpVersion | $AppMcp$ |
| SkiaSharpVersion | $SkiaSharp$ |
| HarfBuzzSharpVersion | $HarfBuzzSharp$ |
| SvgSkiaVersion | $SvgSkia$ |
| WinAppSdkVersion | $WinAppSdk$ |
| WinAppSdkBuildToolsVersion | $WinAppSdkBuildTools$ |
| WinAppSdkBuildToolsWinAppVersion | $WinAppSdkBuildToolsWinApp$ |
| MicrosoftLoggingVersion** | $MicrosoftLoggingConsole$ |
| MicrosoftIdentityClientVersion | $MsalClient$ |
| MicrosoftWebView2Version | $MicrosoftWebView2$ |
| VlcNativeWindowsAssetsVersion | $VlcNativeWindowsAssets$ |
| CommunityToolkitMvvmVersion | $Mvvm$ |
| AndroidMaterialVersion | $AndroidMaterial$ |
| AndroidXSplashScreenVersion | $AndroidXSplashScreen$ |
| AndroidXAppCompatVersion | $AndroidXAppCompat$ |
| AndroidXRecyclerViewVersion | $AndroidXRecyclerView$ |
| AndroidXActivityVersion | $AndroidXActivity$ |
| AndroidXBrowserVersion | $AndroidXBrowser$ |
| AndroidXSwipeRefreshLayoutVersion | $AndroidXSwipeRefreshLayout$ |
| AndroidXLeanbackVersion | $AndroidXLeanback$ |
| AndroidXCarAppVersion | $AndroidXCarApp$ |
| AndroidXWearVersion | $AndroidXWear$ |
| AndroidXWearTilesVersion | $AndroidXWearTiles$ |
| AndroidXNavigationVersion | $AndroidXNavigation$ |
| AndroidXCollectionVersion | $AndroidXCollection$ |
| AndroidXFragmentVersion | $AndroidXFragment$ |
| AndroidXLifecycleVersion | $AndroidXLifecycle$ |
| MauiVersion** | $Maui$ |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 11.0.

```json
$PackagesJson$
```
