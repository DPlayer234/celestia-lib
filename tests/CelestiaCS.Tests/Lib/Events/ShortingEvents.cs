using System.Threading;
using CelestiaCS.Lib.Events;

namespace CelestiaTests.Lib.Events;

public class ShortingEvents
{
    [Test]
    public void Invoke_One()
    {
        ShortingEventCore<EventArgs> shortEvent = default;

        int count = 0;
        shortEvent.Add(a =>
        {
            Assert.That(a.Name == "Hello World!");
            Interlocked.Increment(ref count);
            a.IsHandled = true;
        });

        bool handled = shortEvent.Invoke(new EventArgs("Hello World!"));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(handled, Is.True);
    }

    [Test]
    public void Invoke_Multiple()
    {
        const int HandlerCount = 4;

        ShortingEventCore<EventArgs> shortEvent = default;

        int count = 0;
        for (int i = 0; i < HandlerCount; i++)
        {
            shortEvent.Add(a =>
            {
                Assert.That(a.Name == "Hello World!");
                Interlocked.Increment(ref count);
            });
        }

        bool handled = shortEvent.Invoke(new EventArgs("Hello World!"));
        Assert.That(count, Is.EqualTo(HandlerCount));
        Assert.That(handled, Is.False);
    }

    [Test]
    public void Invoke_Interrupt()
    {
        ShortingEventCore<EventArgs> shortEvent = default;

        int count = 0;
        shortEvent.Add(a =>
        {
            Assert.That(a.Name == "Hello World!");
            Interlocked.Increment(ref count);
            a.IsHandled = true;
        });

        shortEvent.Add(a =>
        {
            Assert.Fail("The handler cancels before this gets called.");
            Interlocked.Increment(ref count);
        });

        bool handled = shortEvent.Invoke(new EventArgs("Hello World!"));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(handled, Is.True);
    }

    [Test]
    public void Invoke_None()
    {
        ShortingEventCore<EventArgs> shortEvent = default;

        shortEvent.Add(HandlerAsync);
        shortEvent.Remove(HandlerAsync);

        bool handled = shortEvent.Invoke(new EventArgs(""));
        Assert.That(handled, Is.False);

        static void HandlerAsync(EventArgs _)
        {
            Assert.Fail("Never executed.");
        }
    }

    public sealed class EventArgs : ShortingEventArgs
    {
        public EventArgs(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
