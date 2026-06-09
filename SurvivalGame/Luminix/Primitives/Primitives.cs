using System;
using System.Collections.Immutable;

namespace Luminix.Primitives;

public interface IValue
{
    object GetValue();
    string ToString();
    Type   GetType();
}

public sealed record class ValueArray<T>(T[] elements) : IValue
{
    public T this[int index] => elements[index];

    public object GetValue()
    {
        return elements;
    }
    public override string ToString()
    {
        return elements.Length == 0 
            ? "ValueArray[]" 
            : $"ValueArray[{string.Join(", ", elements)}]";
    }
}


public interface IValue<T> : IValue, IEquatable<T>
{
    T Value { get; }
    object IValue.GetValue() => Value;
    Type   IValue.GetType() => typeof(T);
    string IValue.ToString() => $"{typeof(T).Name}({Value})";
}

public record class Byte(byte value) : IValue<byte>
{
    public byte Value { get; } = value;
    public static explicit operator byte(Byte wrapper) => wrapper.Value;
    public static explicit operator Byte(byte value) => new Byte(value);
    public bool Equals(byte other) => Value == other;
}
public record class Short(short value) : IValue<short>
{
    public short Value { get; } = value;
    public static explicit operator short(Short wrapper) => wrapper.Value;
    public static explicit operator Short(short value) => new Short(value);
    public bool Equals(short other) => Value == other;
}
public record class Integer(int value) : IValue<int>
{
    public int Value { get; } = value;
    public static explicit operator int(Integer wrapper) => wrapper.Value;
    public static explicit operator Integer(int value) => new Integer(value);
    public bool Equals(int other) => Value == other;
}
public record class Long(long value) : IValue<long>
{
    public long Value { get; } = value;
    public static explicit operator long(Long wrapper) => wrapper.Value;
    public static explicit operator Long(long value) => new Long(value);
    public bool Equals(long other) => Value == other;
}

public record class Float(float value) : IValue<float>
{
    public float Value { get; } = value;
    public static explicit operator float(Float wrapper) => wrapper.Value;
    public static explicit operator Float(float value) => new Float(value);
    public bool Equals(float other) => Value == other;
}
public record class Double(double value) : IValue<double>
{
    public double Value { get; } = value;
    public static explicit operator double(Double wrapper) => wrapper.Value;
    public static explicit operator Double(double value) => new Double(value);
    public bool Equals(double other) => Value == other;
}

public record class Boolean(bool value) : IValue<bool>
{
    public bool Value { get; } = value;
    public static explicit operator bool(Boolean wrapper) => wrapper.Value;
    public static explicit operator Boolean(bool value) => new Boolean(value);
    public bool Equals(bool other) => Value == other;
}
public record class Character(char value) : IValue<char>
{
    public char Value { get; } = value;
    public static explicit operator char(Character wrapper) => wrapper.Value;
    public static explicit operator Character(char value) => new Character(value);
    public bool Equals(char other) => Value == other;
}

public record class Text(string value) : IValue<string>
{
    public string Value { get; } = value;
    public static explicit operator string(Text wrapper) => wrapper.Value;
    public static explicit operator Text(string value) => new Text(value);
    public bool Equals(string other) => Value == other;
}

