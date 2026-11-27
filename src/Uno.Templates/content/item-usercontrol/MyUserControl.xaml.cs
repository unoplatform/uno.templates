//-:cnd:noEmit
namespace $rootnamespace$;

public sealed partial class $safeitemname$ : UserControl
{
    public $safeitemname$()
    {
//+:cnd:noEmit
#if useCsharpMarkup
        this.Content(new Grid());
#else
        this.InitializeComponent();
#endif
    }
}
