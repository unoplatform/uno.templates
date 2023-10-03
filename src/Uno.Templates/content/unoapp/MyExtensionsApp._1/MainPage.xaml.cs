//-:cnd:noEmit
namespace MyExtensionsApp._1;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this
#if useMaterial
            .Background(Theme.Brushes.Background.Default)
#else
            .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            .Content(new StackPanel()
            .VerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .Text("Hello Uno Platform!")
            ));
#else
        this.InitializeComponent();
#endif
//-:cnd:noEmit
    }
}
