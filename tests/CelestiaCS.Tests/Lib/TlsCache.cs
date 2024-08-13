using System.Runtime.CompilerServices;

namespace CelestiaTests.Lib;

using BoxCache = TlsPerCoreCache<StrongBox<int>, TlsCache>;

public class TlsCache
{
    [Test, RequiresThread]
    public void RentingMany()
    {
        var a = BoxCache.Rent() ?? new(0);
        var b = BoxCache.Rent() ?? new(1);
        var c = BoxCache.Rent() ?? new(2);

        BoxCache.Return(a);
        BoxCache.Return(b);
        BoxCache.Return(c);

        var a2 = BoxCache.Rent();
        var b2 = BoxCache.Rent();
        var c2 = BoxCache.Rent();

        Assert.That(new[] { a, b }, Is.EquivalentTo(new[] { a2, b2 }), "Expect to rent the same 2 as were first returned.");
        Assert.That(a, Is.Not.AnyOf(b, c, b2, c2));
        Assert.That(b, Is.Not.AnyOf(a, c, a2, c2));
        Assert.That(c, Is.Not.AnyOf(a, b, a2, b2, c2));
        Assert.That(c2, Is.Not.AnyOf(a, b, c, a2, b2));
    }
}
