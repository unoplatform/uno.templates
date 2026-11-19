# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

<!-- VersionProperties:Start -->
<!-- Written by the Uno.Sdk updater from the "versionProperty" of each group in packages.json
     (Services/ReadMeTable.cs). The rows below are only kept for an Uno.Sdk.Private without it. -->

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | $Core$ |
| UnoExtensionsVersion | $Extensions$ |
| UnoToolkitVersion | $Toolkit$ |
| UnoThemesVersion | $Themes$ |
| UnoCSharpMarkupVersion | $CSharpMarkup$ |
| UnoReactorVersion | $Reactor$ |
| UnoWasmBootstrapVersion** | $WasmBootstrap$ |
| UnoLoggingVersion | $OSLogging$ |
| UnoCoreLoggingSingletonVersion | $CoreLogging$ |
| UnoDspTasksVersion | $Dsp$ |
| UnoResizetizerVersion | $Resizetizer$ |
| SkiaSharpVersion | $SkiaSharp$ |
| SvgSkiaVersion | $SvgSkia$ |
| WinAppSdkVersion | $WinAppSdk$ |
| WinAppSdkBuildToolsVersion | $WinAppSdkBuildTools$ |
| WinAppSdkBuildToolsWinAppVersion | $WinAppSdkBuildToolsWinApp$ |
| MicrosoftLoggingVersion** | $MicrosoftLoggingConsole$ |
| MicrosoftIdentityClientVersion | $MsalClient$ |
| CommunityToolkitMvvmVersion | $Mvvm$ |
| PrismVersion | $Prism$ |
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

<!-- VersionProperties:End -->

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 11.0.

```json
$PackagesJson$
```
