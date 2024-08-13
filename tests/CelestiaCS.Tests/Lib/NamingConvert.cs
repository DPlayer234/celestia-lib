using CelestiaCS.Lib.Format;

namespace CelestiaTests.Lib;

public class NamingConvert
{
    [TestCase("camelCase", "camel_case")]
    [TestCase("PascalCase", "pascal_case")]
    [TestCase("Aah", "aah")]
    [TestCase("AaaaBbbbCccc", "aaaa_bbbb_cccc")]
    public void CamelToSnake(string input, string result)
    {
        Assert.That(Naming.CamelToSnakeCase(input), Is.EqualTo(result));
    }

    [TestCase("camelCase", "camel-case")]
    [TestCase("PascalCase", "pascal-case")]
    [TestCase("Aah", "aah")]
    [TestCase("AaaaBbbbCccc", "aaaa-bbbb-cccc")]
    public void CamelToKebap(string input, string result)
    {
        Assert.That(Naming.CamelToKebabCase(input), Is.EqualTo(result));
    }

    [TestCase("snake_case", "snakeCase")]
    [TestCase("aah", "aah")]
    [TestCase("aaaa_bbbb_cccc", "aaaaBbbbCccc")]
    public void SnakeToCamel(string input, string result)
    {
        Assert.That(Naming.SnakeToCamelCase(input), Is.EqualTo(result));
    }

    [TestCase("kebap-case", "kebapCase")]
    [TestCase("aah", "aah")]
    [TestCase("aaaa-bbbb-cccc", "aaaaBbbbCccc")]
    public void KebapToCamel(string input, string result)
    {
        Assert.That(Naming.KebabToCamelCase(input), Is.EqualTo(result));
    }

    [TestCase("camelCase", "camel Case")]
    [TestCase("PascalCase", "Pascal Case")]
    [TestCase("Aah", "Aah")]
    [TestCase("AaaaBbbbCccc", "Aaaa Bbbb Cccc")]
    public void SpaceCamel(string input, string result)
    {
        Assert.That(Naming.SpaceCamelCase(input), Is.EqualTo(result));
    }
}
