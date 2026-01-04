using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public abstract class Entity
{
    public Entity() { }
    protected EntityType type { private set; get; }
    protected uint id { private set; get; }
    protected EntityStaticTags staticTags = new();
    protected HashSet<ushort> tags = new();

    internal void Initialize(EntityType type, uint id)
    {
        this.type = type;
        this.id = id;
    }

    public abstract void Awake(); 
    public abstract void Start();

    public bool HasTag(ushort tagId)
    {
        return staticTags.tags.Contains(tagId) || tags.Contains(tagId);
    }
    public bool HasTag(string tag)
    {
        return Entities.TryGetTag(tag, out ushort tagId) && HasTag(tagId);
    }

    public bool AddTag(ushort tagId)
    {
        if (Entities.EntityTagCount <= tagId)
            return false;

        return tags.Add(tagId);
    }
    public bool AddTag(string tag)
    {
        if(Entities.TryGetTag(tag, out ushort tagId))
            return tags.Add(tagId);

        return false;
    }

    public bool RemTag(ushort tagId)
    {
        return tags.Remove(tagId);
    }
    public bool RemTag(string tag)
    {
        return Entities.TryGetTag(tag, out ushort tagId) && RemTag(tagId);
    }

    public static bool operator ==(Entity a, Entity b) => a.id == b.id && a.type == b.type;
    public static bool operator !=(Entity a, Entity b) => !(a.id == b.id && a.type == b.type);
    
    public override bool Equals(object obj) => obj is EntityType other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(type.Id, id);

    public EntityType GetEntityType()
    {
        return type;
    }
    public uint GetEntityId()
    {
        return id;
    }
}

public struct EntityType
{
    public EntityType(ushort typeId)
    {
        this.Id = typeId;
    }
    
    public readonly ushort Id;

    public static bool operator ==(EntityType a, EntityType b) => a.Id == b.Id;
    public static bool operator !=(EntityType a, EntityType b) => a.Id != b.Id;

    public bool Equals(EntityType other) => Id == other.Id;

    public override bool Equals(object obj) => obj is EntityType other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => $"EntityType({Id})";
}

public class EntityStaticTags
{
    public HashSet<ushort> tags;
}