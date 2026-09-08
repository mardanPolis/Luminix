using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Luminix.Collections;

namespace Luminix;

internal class ComponentType<T> : IComponentType where T : struct
{
    public static ComponentType<T> NewDefault() => new ComponentType<T>() { data = default };

    public Type Type => typeof(T);
    public int Size => Unsafe.SizeOf<T>();
    public T Default { get { return data; } set { data = value; } }
    [JsonInclude]
    internal T data;

    public IComponentContainer GetComponentContainer()
    {
        return new ComponentContainer<T>(this);
    }

    public override string ToString()
    {
        return Default.ToString();
    }
}