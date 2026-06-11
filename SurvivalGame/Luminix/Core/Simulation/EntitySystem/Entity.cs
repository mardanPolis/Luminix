using Microsoft.Xna.Framework;
using Luminix.Identifiers;
using System;

namespace Luminix;

public struct Entity : IEquatable<Entity>
{
    public ID ID { get; }
    public Variant Variant { get; }
    public bool Enabled { get; set; }
    public Transform Transform { get; set; }

    //TODO : finish methods Create, GetTransform, Delete and Component Methods
    public static ID Create(Variant variant)
    {
        throw new NotImplementedException(nameof(Create));
    }

    public static Transform GetTransform(ID entityID)
    {
        throw new NotImplementedException(nameof(GetTransform));
    }

    public static bool HasComponent<T>(ID entityID) where T : struct
    {
        throw new NotImplementedException(nameof(HasComponent));
    }

    public static bool TryGetComponent<T>(ID entityID, out T _component) where T : struct
    {
        throw new NotImplementedException(nameof(TryGetComponent));
    }

    public static void Destroy(ID entityID)
    {
        throw new NotImplementedException(nameof(Destroy));
    }

    public bool Equals(Entity other)
    {
        return ID.Value == other.ID.Value;
    }
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
    public override string ToString()
    {
        return $"Entity{ID}";
    }
}
