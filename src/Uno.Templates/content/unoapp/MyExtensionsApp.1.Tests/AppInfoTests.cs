//-:cnd:noEmit
namespace MyExtensionsApp._1.Tests;

public class AppInfoTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AppInfoCreation()
    {
        var appInfo = new AppConfig { Environment = "Test" };
#if(useFluentAssertions)
        appInfo.Should().NotBeNull();
        appInfo.Environment.Should().Be("Test");
#endif
#if(useShouldly)
        appInfo.ShouldNotBeNull();
        appInfo.Environment.ShouldBe("Test");
#endif
#if(!useShouldly && !useFluentAssertions)
        Assert.That(appInfo, Is.Not.Null);
        Assert.That(appInfo.Environment, Is.EqualTo("Test"));
#endif
    }
}
