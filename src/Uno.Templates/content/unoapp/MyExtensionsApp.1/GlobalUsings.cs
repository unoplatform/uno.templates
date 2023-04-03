//-:cnd:noEmit
global using System.Collections.Immutable;
global using Microsoft.Extensions.DependencyInjection;
//+:cnd:noEmit
#if (useDependencyInjection)
global using Microsoft.Extensions.Hosting;
#endif
#if (useLocalization)
global using Microsoft.Extensions.Localization;
#endif
global using Microsoft.Extensions.Logging;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
#if (useCsharpMarkup)
global using Microsoft.UI.Xaml.Automation;
global using Microsoft.UI.Xaml.Data;
#endif
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Navigation;
#if (useConfiguration)
global using Microsoft.Extensions.Options;
#endif
#if (useBusinessModelsNamespace)
global using MyExtensionsApp._1.Business.Models;
#endif
#if (useInfrastructureNamespace)
global using MyExtensionsApp._1.Infrastructure;
#endif
#if (useExtensionsNavigation)
global using MyExtensionsApp._1.Presentation;
#endif
#if (useHttp)
global using MyExtensionsApp._1.DataContracts;
global using MyExtensionsApp._1.DataContracts.Serialization;
global using MyExtensionsApp._1.Services.Caching;
global using MyExtensionsApp._1.Services.Endpoints;
global using Uno.Extensions.Http;
#endif
#if (useExtensionsNavigation)
global using Uno.Extensions.Navigation;
#endif
#if (useHttp)
global using Refit;
#endif
#if (useDependencyInjection)
global using Uno.Extensions;
#endif
#if (useConfiguration)
global using Uno.Extensions.Configuration;
#endif
#if (useDependencyInjection)
global using Uno.Extensions.Hosting;
#endif
#if (useLocalization)
global using Uno.Extensions.Localization;
#endif
#if (useLogging)
global using Uno.Extensions.Logging;
#endif
#if (useCsharpMarkup)
global using Uno.Extensions.Markup;
#if (useMaterial)
global using Uno.Material;
#endif
global using Uno.Themes.Markup;
#if (useToolkit)
global using Uno.Toolkit.UI;
#if (useMaterial)
global using Uno.Toolkit.UI.Material;
#endif
#endif
#endif
global using Windows.ApplicationModel;
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
#if (useCsharpMarkup)
global using Color = Windows.UI.Color;
#endif
//-:cnd:noEmit
