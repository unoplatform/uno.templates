# Uno.Sdk

The Uno.Sdk powers the Uno Platform Single Project, including the ability to implicitly and easily manage many commonly used NuGet Packages with your Uno Platform application. Below is a table of the MSBuild Properties which you can use to override the default versions provided by this version of the SDK. You will also find the full Package manifest to give you a better idea of the packages' versions, or to better understand which variable to use for the packages that you want to override.

| MSBuild Property | Default Version |
|----------------|:---------------:|
| UnoVersion* | $Core$ |
| UnoExtensionsVersion | $Extensions$ |
| UnoToolkitVersion | $Toolkit$ |
| UnoThemesVersion | $Themes$ |
| UnoCSharpMarkupVersion | $CSharpMarkup$ |
| UnoWasmBootstrapVersion** | $UnoWasmBootstrapVersion$ |
| UnoLoggingVersion | $OSLogging$ |
| UnoCoreLoggingSingletonVersion | $CoreLogging$ |
| UnoUniversalImageLoaderVersion | $UniversalImageLoading$ |
| UnoDspTasksVersion | $Dsp$ |
| UnoResizetizerVersion | $Resizetizer$ |
| SkiaSharpVersion | $SkiaSharp$ |
| SvgSkiaVersion | $SvgSkia$ |
| WinAppSdkVersion | $WinAppSdk$ |
| WinAppSdkBuildToolsVersion | $WinAppSdkBuildTools$ |
| MicrosoftLoggingVersion** | $MicrosoftLoggingConsole$ |
| WindowsCompatibilityVersion** | $WindowsCompatibility$ |
| MicrosoftIdentityClientVersion | $MsalClient$ |
| CommunityToolkitMvvmVersion | $Mvvm$ |
| PrismVersion | $Prism$ |
| AndroidMaterialVersion | $AndroidMaterial$ |
| AndroidXLegacySupportV4Version | $AndroidXLegacySupportV4$ |
| AndroidXAppCompatVersion | $AndroidXAppCompat$ |
| AndroidXRecyclerViewVersion | $AndroidXRecyclerView$ |
| AndroidXActivityVersion | $AndroidXActivity$ |
| AndroidXBrowserVersion | $AndroidXBrowser$ |
| AndroidXSwipeRefreshLayoutVersion | $AndroidXSwipeRefreshLayout$ |
| AndroidXNavigationVersion | $AndroidXNavigation$ |
| AndroidXCollectionVersion | $AndroidXCollection$ |
| MauiVersion** | $Maui$ |

\* UnoVersion cannot be changed via MSBuild. You must change the SDK Version to change the UnoVersion.
\*\* This version may have a different version for .NET 9.0.

```json
$PackagesJson$
```
