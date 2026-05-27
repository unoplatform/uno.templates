//-:cnd:noEmit
namespace $rootnamespace$;

/// <summary>
/// An empty window that can be activated on its own (e.g. a secondary window of the app).
/// </summary>
public sealed partial class $safeitemname$ : Window
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(
            new Grid()
                .Children(
                    new TextBlock()
                        .Text("Hello Uno Platform!")
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)));
#else
        this.InitializeComponent();
#endif
    }
}
