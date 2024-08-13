using CelestiaCS.Lib.Format;

namespace CelestiaTests.Lib;

public class Numerals
{
    [Test]
    public void Roman()
    {
        var cases = new[]
        {
            (1, "I"),
            (2, "II"),
            (3, "III"),
            (4, "IV"),
            (5, "V"),
            (6, "VI"),
            (7, "VII"),
            (8, "VIII"),
            (9, "IX"),
            (10, "X"),
            (42, "XLII"),
            (69, "LXIX"),
            (271, "CCLXXI"),
            (999, "CMXCIX"),
            (3888, "MMMDCCCLXXXVIII")
        };

        Assert.Multiple(() =>
        {
            foreach (var (value, str) in cases)
            {
                Assert.That(RomanNumerals.ToString(value), Is.EqualTo(str));
            }
        });

        for (int i = 1; i <= 200; i++)
        {
            Console.WriteLine($"{i,3} >> {RomanNumerals.Interpolate(i)}");
        }

        Console.WriteLine();

        for (int i = RomanNumerals.MinSupported; i <= RomanNumerals.MaxSupported; i++)
        {
            Console.WriteLine($"{i,5} >> {RomanNumerals.Interpolate(i)}");
        }
    }
}
