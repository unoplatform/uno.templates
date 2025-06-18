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
#if (useConfiguration)
global using Microsoft.Extensions.Options;
#endif
#if (useBusinessModelsNamespace)
global using MyExtensionsApp._1.Models;
#endif
#if (useExtensionsNavigation)
global using MyExtensionsApp._1.Presentation;
#endif
#if useHttp
global using MyExtensionsApp._1.Services.Endpoints;
#endif
#if (useServer && (useHttpRefit || useHttpKiota))
global using MyExtensionsApp._1.DataContracts;
global using MyExtensionsApp._1.DataContracts.Serialization;
global using MyExtensionsApp._1.Services.Caching;
#endif
#if useHttpKiota && useServer
global using MyExtensionsApp._1.Client;
#endif
#if useHttpKiota
global using Uno.Extensions.Http.Kiota;
#endif

#if (mauiEmbedding)
//-:cnd:noEmit
#if MAUI_EMBEDDING
global using MyExtensionsApp._1.MauiControls;
#endif
//+:cnd:noEmit
#endif
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
#if (useCsharpMarkup)
global using Color = Windows.UI.Color;
#endif
#if (useMvvm)
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
#endif
#if (useMvux)
[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]
#endif
//-:cnd:noEmit
