namespace CelestiaTests;

public static class AssertEx
{
    public static void DebugOnly()
    {
#if !DEBUG
        Assert.Inconclusive("This test is only supported in DEBUG builds.");
#endif
    }

    public static void ExcludeCodeCoverage()
    {
#if CODECOVERAGE
        Assert.Inconclusive("This test is excluded when determining coverage.");
#endif
    }

    public static Task Todo()
    {
        Assert.Inconclusive("Test is TODO.");
        return Task.CompletedTask;
    }
}
