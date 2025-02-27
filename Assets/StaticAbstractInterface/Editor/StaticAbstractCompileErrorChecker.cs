using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class StaticAbstractCompileErrorChecker 
{
    [InitializeOnLoadMethod]
    public static void OnCompiled()
    {
        Type[] types = TypeCache.GetMethodsWithAttribute<StaticAbstractAttribute>()
            .Where((method) => method.IsStatic && method.DeclaringType.IsInterface)
            .Select((method) => method.DeclaringType).Distinct().ToArray();

        foreach (Type interfaceType in types)
        {
            MethodInfo[] methods = interfaceType.GetMethods().Where((method) => method.GetCustomAttribute<StaticAbstractAttribute>() != null).ToArray();
            Type[] derivedTypes = TypeCache.GetTypesDerivedFrom(interfaceType).Where((type) => !type.IsAbstract).ToArray();

            foreach (Type derivedType in derivedTypes)
            {
                foreach (MethodInfo method in methods)
                {
                    Type[] paramTypes = method.GetParameters().Select((param) => param.ParameterType).ToArray();
                    Type[] genericTypes = method.GetGenericArguments();

                    Type searchType = derivedType;
                    MethodInfo overrideMethod = searchType.GetMethod(method.Name, genericTypes.Length, paramTypes);
                    while (overrideMethod == null && searchType != null)
                    {
                        searchType = searchType.BaseType;
                        if (searchType.GetInterface(interfaceType.Name) == null)
                        {
                            break;
                        }

                        overrideMethod = searchType?.GetMethod(method.Name, genericTypes.Length, paramTypes);
                    }


                    if (overrideMethod == null || overrideMethod.ReturnType != method.ReturnType || 
                        !overrideMethod.IsPublic || !overrideMethod.IsStatic)
                    {
                        Debug.LogError($"{derivedType.Name} must declare {method.Name} from {interfaceType.Name}");
                    }
                }
            }
        }
    }
}
