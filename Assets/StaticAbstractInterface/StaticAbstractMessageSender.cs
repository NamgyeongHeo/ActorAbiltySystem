using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public static class StaticAbstractMessageSender<T>
{
    public static bool Call<DerivedType>(Type[] genericTypes, ref object ret, string name, params object[] args) where DerivedType : T
    {
        return Call(typeof(DerivedType), genericTypes, ref ret, name, args);
    }

    public static bool Call(Type derivedType, Type[] genericTypes, ref object ret, string name, params object[] args)
    {
        Type interfaceType = typeof(T);
        if (derivedType.GetInterface(interfaceType.Name) == null || !interfaceType.IsInterface)
        {
            return false;
        }

        MethodInfo method = derivedType.GetMethod(name, genericTypes != null ? genericTypes.Length : 0, args.Select((arg) => arg.GetType()).ToArray());
        while (method == null && derivedType != null)
        {
            derivedType = derivedType.BaseType;
            if (derivedType.GetInterface(interfaceType.Name) == null)
            {
                break;
            }

            method = derivedType?.GetMethod(name, genericTypes != null ? genericTypes.Length : 0, args.Select((arg) => arg.GetType()).ToArray());
        }

        if (genericTypes != null && genericTypes.Length > 0)
        {
            method = method.MakeGenericMethod(genericTypes);
        }

        if (method == null)
        {
            return false;
        }

        ret = method.Invoke(null, args);

        return true;
    }
}