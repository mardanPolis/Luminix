using Microsoft.Xna.Framework;
using Luminix.Identifiers;
using System;

namespace Luminix;

public struct Entity
{
    public readonly record struct TypeID : IEquatable<TypeID>
    {
        private readonly uint value;

        public TypeID(uint value)
        {
            this.value = value;
        }

        public uint Value => value;

        public bool Equals(TypeID other) => value == other.value;

        public override int GetHashCode() => (int)value;

        public override string ToString() => $"[Type:{value}]";
    }

    internal Entity(ID iD)
    {
        ID = iD;
    }

    public ID ID { get; }
    public bool Enabled { get; set; }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
    public override string ToString()
    {
        return $"Entity{ID}";
    }
}
