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
        // Window is not a DependencyObject, so C# Markup has no fluent Content() for it.
        this.Content = new Grid()
            .Children(
                new TextBlock()
                    .Text("Hello Uno Platform!")
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center));
#else
        this.InitializeComponent();
#endif
    }
}
