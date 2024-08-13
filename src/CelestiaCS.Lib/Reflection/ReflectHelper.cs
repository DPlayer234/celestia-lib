using System;

namespace CelestiaCS.Lib.Reflection;

public static class ReflectHelper
{
    public static Type? FindInterface(Type implementor, Type intf)
    {
        var implIntfs = implementor.GetInterfaces();
        if (intf.IsGenericTypeDefinition)
        {
            foreach (var implIntf in implIntfs)
            {
                if (!implIntf.IsGenericType) continue;

                if (implIntf.GetGenericTypeDefinition() == intf)
                {
                    return implIntf;
                }
            }
        }
        else
        {
            foreach (var implIntf in implIntfs)
            {
                if (implIntf == intf)
                {
                    return implIntf;
                }
            }
        }

        return null;
    }
}
