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
using Microsoft.UI.Xaml.Media;
//+:cnd:noEmit
#if (!useSkiaRenderer)
using Com.Nostra13.Universalimageloader.Core;
#endif
//-:cnd:noEmit

namespace MyExtensionsApp._1.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
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
        : base(() => new App(), javaReference, transfer)
    {
//+:cnd:noEmit
#if (!useSkiaRenderer)
        ConfigureUniversalImageLoader();
#endif
//-:cnd:noEmit
    }

//+:cnd:noEmit
#if (!useSkiaRenderer)
    private static void ConfigureUniversalImageLoader()
    {
        // Create global configuration and initialize ImageLoader with this config
        ImageLoaderConfiguration config = new ImageLoaderConfiguration
            .Builder(Context)
            .Build();

        ImageLoader.Instance.Init(config);

        ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
    }
#endif
//-:cnd:noEmit
}

