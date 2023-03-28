//-:cnd:noEmit
using Uno.Extensions.Reactive;

namespace MyExtensionsApp._1.Presentation;

public partial record MainModel
{
	public string? Title { get; }

	public IState<string> Name { get; }

//+:cnd:noEmit
#if useLocalization
	public MainModel(
		INavigator navigator,
		IStringLocalizer localizer)
	{
		_navigator = navigator;
		Title = $"Main - {localizer["ApplicationName"]}";
#elif useConfiguration
	public MainModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{
		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";
#else
	public MainModel(INavigator navigator)
	{
		_navigator = navigator;
		Title = "Main - MyExtensionsApp";
#endif
//-:cnd:noEmit
		Name = State<string>.Value(this, () => string.Empty);
	}

	public async Task GoToSecond()
	{
		var name = await Name;
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
	}

	private INavigator _navigator;
}
