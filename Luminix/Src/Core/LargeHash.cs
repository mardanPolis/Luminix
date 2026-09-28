using System;

namespace Luminix;

public readonly struct LargeHash : IEquatable<LargeHash>
{
    private const long Seed1 = unchecked((long)0x9E3779B97F4A7C15);
    private const long Seed2 = unchecked((long)0xC2B2AE3D27D4EB4F);

    /// <summary>
    /// Reference-identity hash for objects (e.g. Type instances). Two different
    /// instances always hash differently, regardless of any GetHashCode override.
    /// </summary>
    public static LargeHash GetHash(ReadOnlySpan<object> items)
    {
        long h1 = items.Length;
        long h2 = Seed2;

        for (int i = 0; i < items.Length; i++)
        {
            long v = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(items[i]);
            h1 = Mix64(h1 ^ v);
            h2 = Mix64(h2 + v + Seed1 + (h2 << 6) + (h2 >> 2));
        }

        return new LargeHash(new UInt128((ulong)h1, (ulong)h2));
    }

    /// <summary>
    /// Specialized identity hash for Type, using RuntimeTypeHandle directly.
    /// Preferred over the object overload for Type spans.
    /// </summary>
    public static LargeHash GetHash(ReadOnlySpan<Type> items)
    {
        long h1 = items.Length;
        long h2 = Seed2;

        for (int i = 0; i < items.Length; i++)
        {
            long v = items[i].TypeHandle.Value.ToInt64();
            h1 = Mix64(h1 ^ v);
            h2 = Mix64(h2 + v + Seed1 + (h2 << 6) + (h2 >> 2));
        }

        return new LargeHash(new UInt128((ulong)h1, (ulong)h2));
    }

    public static LargeHash GetHash<T>(ReadOnlySpan<T> items) where T : struct
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>()
            ? GetHashFromElements(items)
            : GetHashFromBytes(items);
    }

    private static LargeHash GetHashFromElements<T>(ReadOnlySpan<T> items) where T : struct
    {
        long h1 = items.Length;
        long h2 = Seed2;

        for (int i = 0; i < items.Length; i++)
        {
            long v = items[i].GetHashCode();
            h1 = Mix64(h1 ^ v);
            h2 = Mix64(h2 + v + Seed1 + (h2 << 6) + (h2 >> 2));
        }

        return new LargeHash(new UInt128((ulong)h1, (ulong)h2));
    }

    private static LargeHash GetHashFromBytes<T>(ReadOnlySpan<T> items) where T : struct
    {
        long h1 = items.Length;
        long h2 = Seed2;

        ReadOnlySpan<byte> bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(items);
        
        int i = 0;
        for (; i + sizeof(long) <= bytes.Length; i += sizeof(long))
        {
            long v = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(i));
            h1 = Mix64(h1 ^ v);
            h2 = Mix64(h2 + v + Seed1 + (h2 << 6) + (h2 >> 2));
        }

        if (i < bytes.Length)
        {
            long tail = 0;
            for (int j = 0; i < bytes.Length; i++, j++)
                tail |= (long)bytes[i] << (j * 8);

            h1 = Mix64(h1 ^ tail);
            h2 = Mix64(h2 + tail + Seed1 + (h2 << 6) + (h2 >> 2));
        }

        return new LargeHash(new UInt128((ulong)h1, (ulong)h2));
    }


    private static long Mix64(long x)
    {
        unchecked
        {
            x = (x ^ (x >> 30)) * unchecked((long)0xBF58476D1CE4E5B9);
            x = (x ^ (x >> 27)) * unchecked((long)0x94D049BB133111EB);
            x ^= x >> 31;
            return x;
        }
    }

    private LargeHash(UInt128 value)
    {
        this.value = value;
    }

    private readonly UInt128 value;

    public bool Equals(LargeHash other) => value == other.value;
    public override bool Equals(object obj) => obj is LargeHash other && Equals(other);

    public override int GetHashCode() => value.GetHashCode();

    public static bool operator ==(LargeHash left, LargeHash right) => left.Equals(right);
    public static bool operator !=(LargeHash left, LargeHash right) => !left.Equals(right);

    public override string ToString() => value.ToString("X32");

}