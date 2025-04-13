# Getting Started

Welcome to the Uno Platform!

To discover how to get started with your new app: https://aka.platform.uno/get-started

Get a Overview of all the cool Features Uno provides: https://platform.uno/docs/articles/intro.html

For more information on how to use the Uno.Sdk or upgrade Uno Platform packages in your solution: https://aka.platform.uno/using-uno-sdk

## Table of Content

- [Link Collection to the Uno Platform Documentation](#link-collection-to-the-uno-platform-documentation)
  - [General](#general)
  - [WinUI as Base](#winui-as-base)
- [Discover Uno Studio Options for your Application](#discover-uno-studio-options-for-your-application)
  - [Hot Design](#hot-design)
  - [Hot Reload](#hot-reload)
  - [Design to Code](#design-to-code)
- [Learn about `Uno.Extensions`](#learn-about-unoextensions)
- [Uno.Resizetizer](#unoresizetizer)
- [Getting Help](#getting-help)
- [Contributing to Uno Platform](#contributing-to-uno-platform)

## Link Collection to the Uno Platform Documentation

### General

- [Best practices for developing Uno Platform applications](https://platform.uno/docs/articles/best-practices-uno.html)
- [Developing with Uno Platform](https://platform.uno/docs/articles/using-uno-ui.html)
  - [Uno Platform Features](https://platform.uno/docs/articles/supported-features.html)
    - [List of views implemented in Uno](https://platform.uno/docs/articles/implemented-views.html)
  - [Specific considerations for WinAppSDK](https://platform.uno/docs/articles/features/winapp-sdk-specifics.html)
- [How to upgrade Uno Platform NuGet Packages](https://platform.uno/docs/articles/upgrading-nuget-packages.html) here you can also lookup the latest stable release version of the **Uno.Sdk**!
- [Tutorials](https://platform.uno/docs/articles/samples-tutorials-overview.html)
- [Samples](https://platform.uno/docs/articles/external/uno.samples/doc/samples.html)
- [Additional Resources](https://platform.uno/docs/articles/get-started-next-steps.html)
- [Publishing your Uno App](https://platform.uno/docs/articles/uno-publishing-overview.html)

### WinUI as Base

In case you are not only new to Uno Platform, then also never worked with WinUI flavored XAML (in case you want to develope with C#-Markup, note that the Properties are still same structure as they would be with XAML-Markup), you can find here a Collection of links, to start your Journey from:

- [Links to WinUI documentation](https://platform.uno/docs/articles/winui-doc-links.html)
- [WinUI 3 and Uno Platform](https://platform.uno/docs/articles/uwp-vs-winui3.html)

## Discover Uno Studio Options for your Application

**Uno Platform Studio** revolutionizes how developers design, build, and iterate on their applications.

It includes three key tools, each purpose-built to streamline your workflow:

### [Hot Design®](https://platform.uno/docs/articles/studio/Hot%20Design/hot-design-overview.html)**

  The industry-first, runtime visual designer, for cross-platform .NET Applications. Hot Design transforms your running app into a Designer, from any IDE, on any OS, to create polished interfaces with ease.

  [➜ Learn more about Hot Design®](https://platform.uno/docs/articles/studio/Hot%20Design/hot-design-getstarted-guide.html)

### **[Hot Reload](https://platform.uno/docs/articles/studio/Hot%20Reload/hot-reload-overview.html)**

  Reliably update any code in your app and get instant confirmation your changes were applied, with a new Hot Reload Indicator to monitor changes while you develop.

  [➜ Getting Started with Hot Reload](https://platform.uno/docs/articles/studio/Hot%20Reload/get-started-with-hot-reload.html)

### **[Design-to-Code](https://platform.uno/docs/articles/external/figma-docs/download.html)**

  Generate ready-to-use, well-structured XAML or C# Markup directly from your Figma designs with one click, completely eliminating manual design handoff.

  [➜ Learn more about Design-to-Code](https://platform.uno/docs/articles/external/figma-docs/get-started.html)

## Learn about `Uno.Extensions`

- [Authentication](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Authentication/AuthenticationOverview.html)
- [Configuration](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Configuration/ConfigurationOverview.html)
- [Hosting](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Hosting/HostingOverview.html)
- [HTTP](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Http/HttpOverview.html)
- [Localization](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Localization/LocalizationOverview.html)
- [Logging](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Logging/LoggingOverview.html)
- [Navigation](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Navigation/NavigationOverview.html)
- [Serialization](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Serialization/SerializationOverview.html)
- [Storage](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Storage/StorageOverview.html)
- [Validation](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Validation/ValidationOverview.html)
- [C# Markup](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Markup/Overview.html)
- [.NET MAUI Embedding](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Maui/MauiOverview.html)
- [Theme Service](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/ThemeService/HowTo-UseThemeService.html)

## Uno.Resizetizer

> [!NOTE]
> If you're working in Visual Studio, you can use `*.png` and `*.svg` files to your App without the need to convert them using the Uno.Resizetizer, which is included in every Uno App.

To do this, here is a shortened Guide:

1. Add the image file to the **Assets** in the **Solution Explorer** in one of the folders: Images|Icons|Splash (create them if they not already exist).
2. Open the **Properties** window of the file, and ensure the **Build Action** property is set to **`UnoImage`**.

> [!NOTE]
> You can alternatively do this in the `*.csproj` of your App you want to use it in:
>
> ```xml
> <ItemGroup>
>    <UnoImage Include="Assets\Images\*" />
> </ItemGroup>
> ```

For more information on **Uno.Resizetizer** functionalities, visit [Get Started with Uno.Resizetizer](https://platform.uno/docs/articles/external/uno.resizetizer/doc/using-uno-resizetizer.html).

## Getting Help

In case, you might run into issues while developing your Uno App, you can get in Contact with the Core Team and Community mainly via Discord and GitHub.

Depending on how difficult your Issue is, you might get asked for a reproduction project (aka "repro") so someone else can help investigate the Issue source and help you solve it.

Here you can find a Guide on how to do this: https://platform.uno/docs/articles/uno-howto-create-a-repro.html

## Contributing to Uno Platform

Everyone is welcome to contribute to the Uno Platform. Here you'll find useful information for new and returning contributors.

For starters, please read our [Code of Conduct](https://github.com/unoplatform/uno/blob/master/CODE_OF_CONDUCT.md), which sets out our commitment to an open, welcoming, harassment-free community.

If you're wondering where to start, [read about ways to contribute to Uno](https://github.com/unoplatform/uno/blob/master/doc/articles/contributing/ways-to-contribute.md). Or, you can peruse the list of [first-timer-friendly open issues](https://github.com/unoplatform/Uno/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).

For more information to contributing to Uno, visit the Uno Documentation: https://platform.uno/docs/articles/uno-development/contributing-intro.html
