using System;

namespace CelestiaCS.Lib.Format;

/// <summary>
/// A pair of an upper-case and lower-case character that define a letter.
/// </summary>
public readonly struct Letter : IEquatable<Letter>, IEquatable<char>
{
    // Comparing to these properties (e.g. myChar == Letter.E) gets inlined completely!

    public static Letter A => new('A', 'a');
    public static Letter B => new('B', 'b');
    public static Letter C => new('C', 'c');
    public static Letter D => new('D', 'd');
    public static Letter E => new('E', 'e');
    public static Letter F => new('F', 'f');
    public static Letter G => new('G', 'g');
    public static Letter H => new('H', 'h');
    public static Letter I => new('I', 'i');
    public static Letter J => new('J', 'j');
    public static Letter K => new('K', 'k');
    public static Letter L => new('L', 'l');
    public static Letter M => new('M', 'm');
    public static Letter N => new('N', 'n');
    public static Letter O => new('O', 'o');
    public static Letter P => new('P', 'p');
    public static Letter Q => new('Q', 'q');
    public static Letter R => new('R', 'r');
    public static Letter S => new('S', 's');
    public static Letter T => new('T', 't');
    public static Letter U => new('U', 'u');
    public static Letter V => new('V', 'v');
    public static Letter W => new('W', 'w');
    public static Letter X => new('X', 'x');
    public static Letter Y => new('Y', 'y');
    public static Letter Z => new('Z', 'z');

    /// <summary>
    /// Initializes a new instance of the <see cref="Letter"/> struct.
    /// </summary>
    /// <param name="lowerCase"> The lower-case character. </param>
    /// <param name="upperCase"> The upper-case character. </param>
    public Letter(char lowerCase, char upperCase)
    {
        LowerCase = lowerCase;
        UpperCase = upperCase;
    }

    /// <summary> The lower-case character. </summary>
    public char LowerCase { get; }
    /// <summary> The upper-case character. </summary>
    public char UpperCase { get; }

    /// <summary>
    /// Determines if this letter is equal to another letter.
    /// </summary>
    /// <param name="other"> The letter to compare it to. </param>
    /// <returns> If both instances are equal. </returns>
    public bool Equals(Letter other) => LowerCase == other.LowerCase && UpperCase == other.UpperCase;

    /// <summary>
    /// Determines if this letter is represented by a character.
    /// </summary>
    /// <param name="other"> The character to compare it to. </param>
    /// <returns> If this letter is represented by the character. </returns>
    public bool Equals(char other) => LowerCase == other || UpperCase == other;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Letter pair && Equals(pair);
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(LowerCase, UpperCase);

    /// <summary>
    /// Determines if the specified character is a vowel.
    /// </summary>
    /// <param name="c"> The character to check. </param>
    /// <returns> If it is a vowel. </returns>
    public static bool IsVowel(char c)
        => c == A || c == E || c == I || c == O || c == U;

    /// <summary>
    /// Determines if the specified letter is a vowel.
    /// </summary>
    /// <param name="c"> The letter to check. </param>
    /// <returns> If it is a vowel. </returns>
    public static bool IsVowel(Letter c)
        => c == A || c == E || c == I || c == O || c == U;

    public static bool operator ==(Letter left, Letter right) => left.Equals(right);
    public static bool operator !=(Letter left, Letter right) => !left.Equals(right);

    public static bool operator ==(Letter left, char right) => left.Equals(right);
    public static bool operator !=(Letter left, char right) => !left.Equals(right);

    public static bool operator ==(char left, Letter right) => right.Equals(left);
    public static bool operator !=(char left, Letter right) => !right.Equals(left);
}
