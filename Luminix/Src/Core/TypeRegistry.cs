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

    private static Type Resolve(string name)
    {
        int firstDot = name.IndexOf('.');
        if (firstDot < 0)
            throw new ArgumentException(
                $"'{name}' is not valid. Expected format: 'AssemblyName.Namespace.ClassName'.",
                nameof(name));

        string assemblyName = name[..firstDot];
        string typeName = name[(firstDot + 1)..];

        var assembly = LuminixManifest.GetAssembly(assemblyName);

        var type = assembly.GetType(typeName, throwOnError: false)
            ?? assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);

        if (type == null) throw new ArgumentException($"'{name}' — assembly '{assemblyName}' was found, but no type named '{typeName}' exists in it.", nameof(name));

        return type;
    }

    private static string Resolve(Type type)
    {
        string fullName = type.FullName;
        string assemblyName = type.Assembly.GetName().Name;

        return $"{assemblyName}.{fullName}";
    }
}