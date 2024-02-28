//-:cnd:noEmit
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
 //+:cnd:noEmit
#if useMsalAuthentication
using Microsoft.Identity.Client;
#endif

namespace MyExtensionsApp._1.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
#if useMsalAuthentication
    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }
#endif
}
