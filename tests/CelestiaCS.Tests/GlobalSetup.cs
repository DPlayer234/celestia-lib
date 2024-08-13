namespace CelestiaTests;

[SetUpFixture]
public static class GlobalSetup
{
    [OneTimeSetUp]
    public static void SetUp()
    {
        AppHelper.Startup();
    }
}
