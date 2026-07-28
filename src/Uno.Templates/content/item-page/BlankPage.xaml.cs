//-:cnd:noEmit
namespace $rootnamespace$;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class $safeitemname$ : Page
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(
            new StackPanel()
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
                .Children(
                    new TextBlock().Text("Hello Uno Platform!")));
#else
        this.InitializeComponent();
#endif
    }
}
