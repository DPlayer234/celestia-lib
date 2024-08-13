using System.Text.Json;
using CelestiaCS.Lib.State;

namespace CelestiaTests.Lib;

public class StateTypes
{
    [Test]
    public void OneOrMany_Value()
    {
        Assert.Multiple(() =>
        {
            AssertThis(5);
            AssertThis((long?)25L);
            AssertThis("Hell");
        });

        void AssertThis<T>(T value)
        {
            var a = new OneOrMany<T>(value);

            Assert.That(a.Count, Is.EqualTo(1));
            Assert.That(a.Value, Is.EqualTo(value));
            Assert.That(a[0], Is.EqualTo(value));

            Assert.Throws<IndexOutOfRangeException>(() => _ = a[1]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = a[-1]);

            Assert.That(a.AsArray(), Is.EqualTo(new T[] { value }));

            int count = 0;
            foreach (var item in a)
            {
                Assert.That(item, Is.EqualTo(value));
                count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public void OneOrMany_Many()
    {
        Assert.Multiple(() =>
        {
            AssertThis(5, 4, 2, 1);
            AssertThis((long?)25L, 10, 2, 155);
            AssertThis("Hell", "H", "Oh", "no", "AAAaaAag");
        });

        void AssertThis<T>(params T[] values)
        {
            var a = new OneOrMany<T>(values);

            Assert.That(a.Count, Is.EqualTo(values.Length));
            Assert.That(a.Value, Is.EqualTo(values[0]));
            Assert.That(a[0], Is.EqualTo(values[0]));

            Assert.Throws(Is.Null, () => _ = a[1]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = a[-1]);

            Assert.That(a.AsArray(), Is.EqualTo(values));

            int count = 0;
            foreach (var item in a)
            {
                Assert.That(item, Is.EqualTo(values[count]), "Enumerated item does not match.");
                count++;
            }
            Assert.That(count, Is.EqualTo(a.Count), "Length and enumerator count don't match.");
        }
    }

    [Test]
    public void OneOrMany_Empty()
    {
        Assert.Multiple(() =>
        {
            AssertThis(default(OneOrMany<int>));
            AssertThis(default(OneOrMany<long?>));
            AssertThis(default(OneOrMany<string>));

            AssertThis(new OneOrMany<int>(Array.Empty<int>()));
            AssertThis(new OneOrMany<long?>(Array.Empty<long?>()));
            AssertThis(new OneOrMany<string>(Array.Empty<string>()));
        });

        void AssertThis<T>(OneOrMany<T> a)
        {
            Assert.That(a.Count, Is.EqualTo(0));

            Assert.Throws(Is.Null, () => _ = a.Value);
            Assert.That(a.Value, Is.EqualTo(default(T)));

            Assert.Throws<IndexOutOfRangeException>(() => _ = a[0]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = a[1]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = a[-1]);

            Assert.That(a.AsArray(), Is.EqualTo(Array.Empty<T>()));

            foreach (var item in a)
            {
                Assert.Fail("Nothing to iterate.");
            }
        }
    }

    [Test]
    public void OneOrMany_Json()
    {
        Assert.Multiple(() =>
        {
            Try(new OneOrMany<int>(5), "5");
            Try(new OneOrMany<int>(new[] { 1, 2, 3 }), "[1,2,3]");
            Try(default(OneOrMany<int>), "[]");

            Try(new OneOrMany<string>("hello"), "\"hello\"");
            Try(new OneOrMany<string>(new[] { "a", "b, c", "d" }), "[\"a\",\"b, c\",\"d\"]");
            Try(default(OneOrMany<string>), "[]");
        });

        static void Try<T>(OneOrMany<T> value, string json)
        {
            Assert.That(JsonSerializer.Serialize(value), Is.EqualTo(json));
            Assert.That(JsonSerializer.Deserialize<OneOrMany<T>>(json), Is.EqualTo(value));
        }
    }
}
