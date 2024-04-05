//-:cnd:noEmit
namespace MyExtensionsApp._1.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public Shell()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(
            new Border()
                .Child(
    #if useToolkit
                    new ExtendedSplashScreen()
                        .Name(out var splash)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .VerticalAlignment(VerticalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .VerticalContentAlignment(VerticalAlignment.Stretch)
                        .LoadingContentTemplate<object>(_ => new Grid()
                            .RowDefinitions("2*,*")
                            .Children(
                                new ProgressRing()
                                    .Grid(row: 1)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .Height(100)
                                    .Width(100)
                            )
                        )
    #else
                    new ContentControl()
                        .Name(out var splash)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .VerticalAlignment(VerticalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                        .VerticalContentAlignment(VerticalAlignment.Stretch)
    #endif
                )
#if useMaterial
                .Background(Theme.Brushes.Background.Default)
#else
                .Background(ThemeResource.Get<Brush>("$themeBackgroundBrush$"))
#endif
            );
        ContentControl = splash;
#else
        this.InitializeComponent();
#endif
//-:cnd:noEmit
    }
//+:cnd:noEmit
#if useCsharpMarkup

    public ContentControl ContentControl { get; }
#else
    public ContentControl ContentControl => Splash;
#endif
//-:cnd:noEmit
}
