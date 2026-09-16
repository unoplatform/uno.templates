//-:cnd:noEmit
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;
using Microsoft.Identity.Client;

namespace MyExtensionsApp._1.Droid;

/// <summary>
/// Catches the redirect back from the browser after an MSAL sign-in.
/// </summary>
/// <remarks>
/// <para>
/// MSAL's Android convention is <c>msal{ClientId}://auth</c>, and that is what the MSAL provider
/// uses unless a <c>RedirectUri</c> is set in the <c>MsalAuthentication</c> configuration section.
/// Android needs an intent filter matching it, or the browser has nowhere to deliver the code and
/// the sign-in never completes - it hangs rather than failing.
/// </para>
/// <para>
/// <b><see cref="RedirectScheme"/> must be updated to match your client id.</b> Intent filters are
/// declared with attributes, which only accept compile-time constants, so this is the one value
/// that cannot follow appsettings.json automatically.
/// </para>
/// </remarks>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = RedirectScheme,
    DataHost = "auth")]
public class MsalActivity : BrowserTabActivity
{
    /// <summary>
    /// "msal" followed by the ClientId from the MsalAuthentication section of appsettings. The
    /// placeholder below keeps the generated app building; replace it with your own client id, and
    /// register <c>msal{ClientId}://auth</c> as a redirect URI on the app registration.
    /// </summary>
    private const string RedirectScheme = "msal00000000-0000-0000-0000-000000000000";
}
