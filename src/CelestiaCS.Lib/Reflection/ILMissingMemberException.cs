using System;

namespace CelestiaCS.Lib.Reflection;

public sealed class ILMissingMemberException : Exception
{
    public ILMissingMemberException(Type searchedType, string memberName, ILMissingMemberType memberType)
        : base(GetExceptionMessage(searchedType, memberName, memberType))
    {
        SearchedType = searchedType;
        MemberName = memberName;
        MemberType = memberType;
    }

    public Type SearchedType { get; }
    public string MemberName { get; }
    public ILMissingMemberType MemberType { get; }

    private static string GetExceptionMessage(Type searchedType, string memberName, ILMissingMemberType memberType)
    {
        return $"{memberType} \"{memberName}\" cannot be found on type \"{searchedType.FullName}\".";
    }
}

public enum ILMissingMemberType
{
    Field,
    Property,
    Method,
    Event,
    Constructor
}
