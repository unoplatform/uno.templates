//-:cnd:noEmit
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : ContentDialog
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Title("Title")
            .PrimaryButtonText("Primary")
            .SecondaryButtonText("Secondary")
            .Content(new Grid());
#else
        this.InitializeComponent();
#endif
    }
#if (!useCsharpMarkup)

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
#endif
}
