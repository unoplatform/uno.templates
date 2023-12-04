//-:cnd:noEmit
namespace MyExtensionsApp._1.MauiControls;

public partial class EmbeddedControl : ContentView
{
    public EmbeddedControl()
    {
        InitializeComponent();
    }

//+:cnd:noEmit
#if (!useMauiMvvmOrMvux)
    private int count=0;

    private void CounterClicked(object sender, EventArgs e)
    {
        CounterButton.Text = ++count switch
        {
            1 => "Pressed Once!",
            _ => $"Pressed {count} times!"
        };
    }
#endif
//-:cnd:noEmit
}
