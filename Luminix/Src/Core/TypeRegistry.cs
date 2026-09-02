using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Luminix;

public static class TypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type> types = new();
    private static readonly ConcurrentDictionary<Type, string> typeNames = new();
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
        return;
    }

    public static void Add(Type type, string name)
    {
        if(typeNames.ContainsKey(type))
            return;

        types.TryAdd(name, type);
        typeNames.TryAdd(type, name);
        return;
    }

    private static Type Resolve(string name)
    {
        int firstDot = name.IndexOf('.');
        if (firstDot < 0)
            throw new ArgumentException(
                $"'{name}' is not valid. Expected format: 'Namespace.ClassName'. First namespace - assembly name.",
                nameof(name));

        string assemblyName = name[..firstDot];

        var assembly = LuminixManifest.GetAssembly(assemblyName);

        var type = assembly.GetType(name, throwOnError: false)
            ?? assembly.GetTypes().FirstOrDefault(t => t.Name == name);

        if (type == null) throw new ArgumentException($"'{name}' — assembly '{assemblyName}' was found, but no type named '{name}' exists in it.", nameof(name));

        return type;
    }

    private static string Resolve(Type type)
    {
        string fullName = type.FullName;
        string assemblyName = type.Assembly.GetName().Name;

        return $"{assemblyName}.{fullName}";
    }
}