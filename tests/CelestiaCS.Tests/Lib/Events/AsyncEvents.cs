using System.Threading;
using CelestiaCS.Lib.Events;

namespace CelestiaTests.Lib.Events;

public class AsyncEvents
{
    [Test]
    public async Task Invoke_One()
    {
        AsyncEventCore<EventArgs> asyncEvent = default;

        int count = 0;
        asyncEvent.Add(async a =>
        {
            await Task.Delay(10);
            Assert.That(a.Name == "Hello World!");
            Interlocked.Increment(ref count);
        });

        await asyncEvent.InvokeAsync(new EventArgs("Hello World!"));
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Invoke_Multiple()
    {
        const int HandlerCount = 4;

        AsyncEventCore<EventArgs> asyncEvent = default;

        int count = 0;
        for (int i = 0; i < HandlerCount; i++)
        {
            int delay = i * 10;
            asyncEvent.Add(async a =>
            {
                await Task.Delay(delay);
                Assert.That(a.Name == "Hello World!");
                Interlocked.Increment(ref count);
            });
        }

        await asyncEvent.InvokeAsync(new EventArgs("Hello World!"));
        Assert.That(count, Is.EqualTo(HandlerCount));
    }

    [Test]
    public async Task Invoke_None()
    {
        AsyncEventCore<EventArgs> asyncEvent = default;

        asyncEvent.Add(HandlerAsync);
        asyncEvent.Remove(HandlerAsync);

        await asyncEvent.InvokeAsync(new EventArgs(""));

        static Task HandlerAsync(EventArgs _)
        {
            Assert.Fail("Never executed.");
            return Task.CompletedTask;
        }
    }

    public sealed class EventArgs
    {
        public EventArgs(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
