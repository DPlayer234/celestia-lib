using CelestiaCS.Lib.Reflection;

namespace CelestiaTests.Lib;

public class ILReflection
{
    [Test]
    public void ConstructorInfo_CreateDelegate()
    {
        var ctor = IL.GetConstructor(typeof(Rec), [typeof(int), typeof(uint), typeof(long)]);
        var @delegate = ctor.CreateDelegate<Func<int, uint, long, Rec>>();
        var rec = @delegate(1, 2, 3);

        Assert.That(rec, Is.EqualTo(new Rec(1, 2, 3)));
    }

    public sealed record Rec(int A, uint B, long C);
}
