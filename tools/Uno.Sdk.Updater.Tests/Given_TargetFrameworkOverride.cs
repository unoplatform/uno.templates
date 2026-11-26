using Uno.Sdk.Updater.Utils;

namespace Uno.Sdk.Updater.Tests;

public class Given_TargetFrameworkOverride
{
    [TestCase("net10.0", "10.0.13", 10)]
    [TestCase("net9.0", "9.0.21", 9)]
    [TestCase("net10.0", "10.1.0-dev.219", 10)]
    [TestCase("net10.0-windows10.0.19041", "10.0.110", 10)]
    // A pin behind its target framework can still catch up to it.
    [TestCase("net10.0", "9.0.19", 10)]
    public void When_Pin_Follows_DotNet_Then_Capped_To_TargetFramework_Major(string key, string pinned, int expected)
        => Assert.That(TargetFrameworkOverride.GetMaxMajor(key, pinned), Is.EqualTo(expected));

    [TestCase("net10.0", "12.1.0")]
    [TestCase("net9.0", "10.0.0-dev.5")]
    public void When_Pin_Above_TargetFramework_Major_Then_No_Cap(string key, string pinned)
        => Assert.That(TargetFrameworkOverride.GetMaxMajor(key, pinned), Is.Null);

    [TestCase("netstandard2.0", "2.0.0")]
    [TestCase("net48", "4.8.0")]
    [TestCase("android", "1.2.3")]
    public void When_Key_Not_DotNet_TargetFramework_Then_No_Cap(string key, string pinned)
        => Assert.That(TargetFrameworkOverride.GetMaxMajor(key, pinned), Is.Null);
}
