using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Uno.UI.Hosting;

namespace MyExtensionsApp._1.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
//+:cnd:noEmit
#if useAndroidTV
    Banner = "@drawable/banner",
#endif
//-:cnd:noEmit
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Uno.UI.Runtime.Android.NativeApplication
{
//+:cnd:noEmit
#if (!useDependencyInjection && useLoggingFallback)
    static Application()
    {
        App.InitializeLogging();
    }
    
#endif
//-:cnd:noEmit
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override UnoPlatformHost CreateHost() =>
        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAndroid()
            .Build();
}
