namespace CelestiaTests.Lib;

public class Throwing
{
    [Test]
    public void NullRefIfNull_Object()
    {
        object? obj = new();
        object? @null = null;

        Assert.Throws(Is.Null, () => ThrowHelper.NullRefIfNull(obj));
        Assert.Throws(Is.TypeOf<NullReferenceException>(), () => ThrowHelper.NullRefIfNull(@null));
    }

    [Test]
    public void NullRefIfNull_Array()
    {
        object[]? obj = new object[16];
        object[]? @null = null;

        Assert.Throws(Is.Null, () => ThrowHelper.NullRefIfNull(obj));
        Assert.Throws(Is.TypeOf<NullReferenceException>(), () => ThrowHelper.NullRefIfNull(@null));
    }

    [Test]
    public void NullRefIfNull_String()
    {
        string? obj = "Hello there.";
        string? @null = null;

        Assert.Throws(Is.Null, () => ThrowHelper.NullRefIfNull(obj));
        Assert.Throws(Is.TypeOf<NullReferenceException>(), () => ThrowHelper.NullRefIfNull(@null));
    }
}
