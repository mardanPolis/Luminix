using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public abstract class Entity
{
    public readonly TypeValues TValues = null;
    public readonly uint Id;
    public Entity(uint id, Entity.TypeValues typeValues)
    {
        Id = id;
        TValues = typeValues;
    }

    protected readonly HashSet<Tag> tags = [];

    public abstract void Awake();
    public abstract void Start();

    public bool HasTag(Tag tag)
    {
        return TValues.HasTag(tag) || tags.Contains(tag);
    }
    public bool HasTag(string tag)
    {
        return Entities.TryGetTag(tag, out Tag tagId) && HasTag(tagId);
    }

    public bool AddTag(Tag tag)
    {
        if (!Entities.TagExists(tag))
            return false;

        return tags.Add(tag);
    }
    public bool AddTag(string tagString)
    {
        if(Entities.TryGetTag(tagString, out Tag tag))
            return tags.Add(tag);

        return false;
    }

    public bool RemTag(Tag tagId)
    {
        return tags.Remove(tagId);
    }
    public bool RemTag(string tagString)
    {
        return Entities.TryGetTag(tagString, out Tag tag) && RemTag(tag);
    }

    public void ClrTags()
    {
        tags.Clear();
    }

    public override bool Equals(object obj) => obj is Entity.Type other && Equals(other);

    public static bool operator ==(Entity a, Entity b) => a.Id == b.Id;
    public static bool operator !=(Entity a, Entity b) => a.Id != b.Id;

    public override int GetHashCode() => HashCode.Combine(GetEntityType().Id, Id);
    public Entity.Type GetEntityType()
    {
        return TValues.Type;
    }
    public uint GetEntityId()
    {
        return Id;
    }

    public struct Type
    {
        public readonly ushort Id;
        public Type(ushort typeId)
        {
            this.Id = typeId;
        }
        
        public static bool operator ==(Entity.Type a, Entity.Type b) => a.Id == b.Id;
        public static bool operator !=(Entity.Type a, Entity.Type b) => a.Id != b.Id;

        public bool Equals(Entity.Type other) => Id == other.Id;

        public override bool Equals(object obj) => obj is Entity.Type other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"Entity.Type[{Id}]";
        public string ToTypeString()
        {
            if(Entities.TryGet(this, out System.Type classType))
                return $"Entity Class[{classType.Name}]";

            throw new NullReferenceException($"Entity.Type references Null Entity.Type with Id[{Id}]");
        }
    }
    public struct Tag
    {
        public readonly ushort Id;
        public Tag(ushort tagId)
        {
            this.Id = tagId;
        }
        
        public static bool operator ==(Entity.Tag a, Entity.Tag b) => a.Id == b.Id;
        public static bool operator !=(Entity.Tag a, Entity.Tag b) => a.Id != b.Id;

        public bool Equals(Entity.Tag other) => Id == other.Id;

        public override bool Equals(object obj) => obj is Entity.Tag other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString()
        {
            if(Entities.TryGetTag(Id, out string tag))
                return tag;

            throw new NullReferenceException($"Tag references Null Tag with Id[{Id}]");
        }
    }
    public class TypeValues
    {
        public readonly Entity.Type Type;
        public TypeValues(Entity.Type type)
        {
            Type = type;
        }

        public Vector2 StartingScale { private set; get; } = Vector2.One;
        internal void SetStartingScale(Vector2 scale)
        {
            StartingScale = scale;
        }

        public HashSet<Tag> Tags { private set; get; } = [];
        public void SetTags(List<Tag> tags) 
        { 
            Tags.Clear();
            Tags.UnionWith(tags);
        }
        public bool AddTag(Tag tag) => Entities.TagExists(tag) && Tags.Add(tag);
        public bool AddTag(string tagString) => Entities.TryGetTag(tagString, out Tag tag) && Tags.Add(tag);
        public bool HasTag(Tag tag) => Tags.Contains(tag);
        public bool HasTag(string tagString) => Entities.TryGetTag(tagString, out Tag tag) && HasTag(tag);
        public bool RemTag(Tag tag) => Tags.Remove(tag);
        public bool RemTag(string tagString) => Entities.TryGetTag(tagString, out Tag tag) && RemTag(tag);
        public void ClrTags() => Tags.Clear();
    } 
}
