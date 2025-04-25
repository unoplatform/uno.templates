//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public sealed partial class SecondPage : Page
{
    public SecondPage()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.DataContext<SecondViewModel>((page, vm) => page
#if useMaterial
            .Background(Theme.Brushes.Background.Default)
#else
            .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            .Content(new Grid()
#if useToolkit
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
#endif
                .Children(
#if useToolkit
                new NavigationBar()
                    .Content("Second Page"),
#else
                new TextBlock()
                    .Text("Second Page")
                    .HorizontalAlignment(HorizontalAlignment.Center),
#endif
                new TextBlock()
                    .Text(() => vm.Entity.Name)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center))));
#else
        this.InitializeComponent();
#endif
    }
}

