namespace CelestiaTests;

public class TestRng : IRng
{
    private static readonly double _maxFactor = Math.BitDecrement(1.0d);

    private readonly LocalRng _rng = new LocalRng();
    private ValueQueue<double> _factorQueue;
    private double _factor;
    private bool _isRandom;

    public TestRng()
    {
        _isRandom = true;
    }

    public bool Chance(double chance)
    {
        if (_isRandom) return _rng.Chance(chance);

        DequeueFactor();
        return _factor < chance;
    }

    public double Float()
    {
        if (_isRandom) return _rng.Float();

        DequeueFactor();
        return _factor;
    }

    public double Float(double min, double max)
    {
        if (_isRandom) return _rng.Float(min, max);

        DequeueFactor();
        return RngMult(_factor, max - min) + min;
    }

    public int Int(int max)
    {
        if (_isRandom) return _rng.Int(max);

        DequeueFactor();
        return RngMult(_factor, max);
    }

    public int Int(int min, int max)
    {
        if (_isRandom) return _rng.Int(min, max);

        DequeueFactor();
        return RngMult(_factor, max - min) + min;
    }

    public void Bytes(Span<byte> buffer)
    {
        if (_isRandom)
        {
            _rng.Bytes(buffer);
            return;
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            DequeueFactor();
            buffer[i] = (byte)RngMult(_factor, byte.MaxValue + 1);
        }
    }

    public TestRng Random()
    {
        _isRandom = true;

        return this;
    }

    /// <summary>
    /// Sets the factor so that chance events never occur (unless they are &gt;= 100%).
    /// </summary>
    public TestRng Never()
    {
        _isRandom = false;
        _factor = _maxFactor;

        return this;
    }

    /// <summary>
    /// Sets the factor so that chance events always occur (unless they are &lt;= 0%).
    /// </summary>
    public TestRng Always()
    {
        _isRandom = false;
        _factor = 0.0d;

        return this;
    }

    /// <summary>
    /// Sets a fixed factor. If the factor is equal to or greater than a chance, the chance will be missed.
    /// </summary>
    /// <param name="factor"> The factor to set. </param>
    public TestRng Fixed(double factor)
    {
        Assert.That(factor,
            Is.LessThan(1.0d) &
            Is.GreaterThanOrEqualTo(0.0d));

        _isRandom = false;
        _factor = factor;

        return this;
    }

    /// <summary>
    /// Sets a fixed factor smaller than given. If the factor given is equal to or less than a chance, the chance will be hit.
    /// </summary>
    /// <param name="factor"> The factor to set. </param>
    public TestRng FixedLow(double factor)
    {
        return Fixed(Math.BitDecrement(factor));
    }

    public TestRng Queue(double factor)
    {
        Assert.That(factor,
            Is.LessThan(1.0d) &
            Is.GreaterThanOrEqualTo(0.0d));

        _isRandom = false;
        _factorQueue.Enqueue(factor);

        return this;
    }

    public TestRng QueueLow(double factor)
    {
        return Queue(Math.BitDecrement(factor));
    }

    public TestRng QueueAlways()
    {
        return Queue(0.0);
    }

    public TestRng QueueNever()
    {
        return Queue(_maxFactor);
    }

    public TestRng EmptyQueue()
    {
        while (_factorQueue.TryDequeue(out _)) ;
        return this;
    }

    private void DequeueFactor()
    {
        if (_factorQueue.TryDequeue(out double newFactor))
            _factor = newFactor;
    }

    private static int RngMult(double factor, int max)
    {
        return (int)(factor * max);
    }

    private static double RngMult(double factor, double max)
    {
        return factor * max;
    }
}
