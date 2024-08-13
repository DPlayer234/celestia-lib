namespace CelestiaTests.Lib;

public class ValueQueues
{
    [Test]
    public void General()
    {
        string[] part1 = ["a", "b", "c", "d", "e"];
        string[] part2 = ["f", "g", "h", "i", "j"];

        ValueQueue<string> queue = default;
        List<string?> temp = new(5);

        for (int i = 0; i < 4; i++)
        {
            foreach (var item in part1)
                queue.Enqueue(item);

            foreach (var item in part2)
                queue.Enqueue(item);

            DequeueCheck(part1);

            foreach (var item in part1)
                queue.Enqueue(item);

            DequeueCheck(part2);

            foreach (var item in part2)
                queue.Enqueue(item);
        }

        Assert.That(queue.Count, Is.EqualTo((part1.Length + part2.Length) * 4));
        DequeueCheck(part1.Concat(part2).Concat(part1).Concat(part2).Concat(part1).Concat(part2).Concat(part1).Concat(part2).ToArray());

        void DequeueCheck(string[] data)
        {
            temp.Clear();
            for (int x = 0; x < data.Length; x++)
            {
                Assert.That(queue.TryDequeue(out var item));
                temp.Add(item);
            }

            Assert.That(temp, Is.EqualTo(data));
        }
    }
}
