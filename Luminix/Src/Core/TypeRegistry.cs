using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Luminix;

namespace Luminix;

public static class TypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> types = new();
    private static readonly ConcurrentDictionary<Type, string> typeNames = new();

    private static readonly ConcurrentDictionary<Type, (Type, bool)> defaultSubTypes = new();
    
    private static readonly ConcurrentDictionary<(Type BaseType, LargeHash), Type> closedGenericTypes = new();
    private static readonly ConcurrentDictionary<Type, Type[]> closedGenericTypeArguments = new();


    public static Type GetType(string name)
    {
        if (types.TryGetValue(name, out var type))
            return type;

        type = Resolve(name);
        types.TryAdd(name, type);
        typeNames.TryAdd(type, name);

        return type;
    }
    public static string GetName(Type type)
    {
        if(typeNames.TryGetValue(type, out var name))
            return name;

        name = Resolve(type);
        types.TryAdd(name, type);
        typeNames.TryAdd(type, name);
        return name;
    }

    public static void Add(Type type)
    {
        if(typeNames.ContainsKey(type))
            return;

        var name = Resolve(type);
        types.TryAdd(name, type);
        typeNames.TryAdd(type, name);
    }

    public static void Add(Type type, string name)
    {
        if(types.ContainsKey(name))
            return;

        types.TryAdd(name, type);
    }

    //SubTypes
    public static void SetDefaultSubType(Type baseType, Type subType, bool forced = false)
    {
        if (baseType == null)
            throw new ArgumentNullException(nameof(baseType), "Base type cannot be null.");

        if (subType == null)
            throw new ArgumentNullException(nameof(subType), "Subtype cannot be null.");

        if (!baseType.IsClass && !baseType.IsInterface)
            throw new ArgumentException($"Base type '{baseType.FullName}' must be a reference class or interface.", nameof(baseType));

        if (!subType.IsClass)
            throw new ArgumentException($"Subtype '{subType.FullName}' must be a reference class.", nameof(subType));

        if (subType.IsAbstract)
            throw new ArgumentException($"Subtype '{subType.FullName}' must not be abstract.", nameof(subType));

        if (!baseType.IsAssignableFrom(subType))
            throw new InvalidOperationException($"Type '{subType.FullName}' is not assignable to '{baseType.FullName}'.");

        defaultSubTypes[baseType] = (subType, forced);

        Add(baseType);
        Add(subType);
    }
    public static Type GetDefaultSubType(Type baseType)
    {
        if (defaultSubTypes.TryGetValue(baseType, out var genericType))
            return genericType.Item1;

        throw new ArgumentException($"No default sub type registered for base type '{baseType.FullName}'.", nameof(baseType));
    }
    public static bool IsDefaultSubTypeForced(Type baseType)
    {
        if (defaultSubTypes.TryGetValue(baseType, out var genericType))
            return genericType.Item2;

        throw new ArgumentException($"No default sub type registered for base type '{baseType.FullName}'.", nameof(baseType));
    }
    public static bool TryGetDefaultSubType(Type baseType, out Type subType)
    {
        if (defaultSubTypes.TryGetValue(baseType, out var entry))
        {
            subType = entry.Item1;
            return true;
        }

        subType = null!;
        return false;
    }
    public static bool TryGetDefaultSubtype(Type baseType, out Type subType, out bool forced)
    {
        if (defaultSubTypes.TryGetValue(baseType, out var entry))
        {
            subType = entry.Item1;
            forced = entry.Item2;
            return true;
        }

        subType = null!;
        forced = false;
        return false;
    }

    //Generic
    public static bool IsGenericType(Type type)
    {
        return type.IsGenericType;
    }
    public static bool IsClosedGenericType(Type type)
    {
        return type.IsGenericType && !type.ContainsGenericParameters;
    }
    public static bool IsOpenGenericType(Type type)
    {
        return type.IsGenericType && type.ContainsGenericParameters;
    }

    public static Type GetClosedGenericType(Type baseType, params ReadOnlySpan<Type> typeArguments)
    {
        if (!IsOpenGenericType(baseType))
            throw new ArgumentException("baseType must be an open generic type definition.", nameof(baseType));
        
        var hash = LargeHash.GetHash(typeArguments);
        var key = (baseType, hash);

        if (closedGenericTypes.TryGetValue(key, out var cached))
            return cached;

        var closedType = baseType.MakeGenericType(typeArguments.ToArray());
        Add(closedType);

        if (!closedGenericTypes.TryAdd(key, closedType))
            closedGenericTypes.TryGetValue(key, out closedType);

        return closedType!;
    }
    public static ReadOnlySpan<Type> GetGenericArguments(Type type)
    {
        if(closedGenericTypeArguments.TryGetValue(type, out var arguments))
            return arguments;

        arguments = type.GetGenericArguments();
        closedGenericTypeArguments.TryAdd(type, arguments);
        return arguments;
    }
    public static Type[] GetGenericArgumentsClone(Type type)
    {
        if(closedGenericTypeArguments.TryGetValue(type, out var arguments))
            return (Type[])arguments.Clone();

        arguments = type.GetGenericArguments();
        closedGenericTypeArguments.TryAdd(type, arguments);
        return arguments;
    }


    private static Type Resolve(string name)
    {
        int firstDot = name.IndexOf('.');
        if (firstDot < 0)
            throw new ArgumentException(
                $"'{name}' is not valid. Expected format: 'Namespace.ClassName'. Root namespace must be assembly name.",
                nameof(name));

        string assemblyName = name[..firstDot];

        var assembly = Application.GetAssembly(assemblyName);

        var type = assembly.GetType(name, throwOnError: false) ?? throw new ArgumentException($"'{name}' — assembly '{assemblyName}' was found, but no type named '{name}' exists in it.", nameof(name));

        return type;
    }

    private static string Resolve(Type type)
    {
        string fullName = type.FullName;
        string assemblyName = type.Assembly.GetName().Name;

        return $"{assemblyName}.{fullName}";
    }
}