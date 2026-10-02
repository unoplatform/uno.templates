//-:cnd:noEmit
#if (useFluentAssertions)
global using FluentAssertions;
#endif
#if (useShouldly)
global using Shouldly;
#endif
global using NUnit.Framework;
global using Uno.UITest;
global using Uno.UITest.Helpers.Queries;
global using Uno.UITests.Helpers;
global using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;
