# Getting Started

Uno 5.1 has introduced a new Uno.Sdk. Among other new features that the Uno.Sdk will begin to light up over the coming months, the Uno.Sdk helps to manage the version of Uno by providing the $(UnoVersion) in MSBuild.

The $(UnoVersion) allows you to easily manage the version of the core Uno packages across your application by simply updating the Uno.Sdk itself. To help better manage this we have added a global.json to your solution that sets the version of the Uno.Sdk to use across your application. You can centrally manage the version of the Uno.Sdk there. At this time the NuGet Manager in Visual Studio does not parse or manage Sdks provided by NuGet. If you would like to see this feature please be sure to provide your feedback [here](https://github.com/NuGet/Home/issues/13127).

Note that the Uno.Sdk is shipped as part of the main Uno repository and is versioned with Uno itself. As a result while it will provide an $(UnoVersion), it cannot and does not currently support versions for Uno.Toolkit.UI, Uno.Extensions, Uno.Themes, Uno.Resizetizer, etc.
