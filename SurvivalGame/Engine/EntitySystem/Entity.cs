using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Engine.EntitySystem;

public abstract class Entity : IDestroyable
{
    internal bool isDestroyed = false;
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

    public virtual void OnDestroy() {}

    public bool HasTag(Tag tag)
    {
        return TValues.HasTag(tag) || tags.Contains(tag);
    }
    public bool HasTag(string tag)
    {
        return EntityTagRegistry.TryGet(tag, out Tag tagId) && HasTag(tagId);
    }

    public bool AddTag(Tag tag)
    {
        if (!EntityTagRegistry.TagExists(tag))
            return false;

        return tags.Add(tag);
    }
    public bool AddTag(string tagString)
    {
        if(EntityTagRegistry.TryGet(tagString, out Tag tag))
            return tags.Add(tag);

        return false;
    }

    public bool RemTag(Tag tag)
    {
        return tags.Remove(tag);
    }
    public bool RemTag(string tagString)
    {
        return EntityTagRegistry.TryGet(tagString, out Tag tag) && tags.Remove(tag);
    }

    public void ClrTags()
    {
        tags.Clear();
    }

    public override bool Equals(object obj) => obj is Entity other && this == other;

    public static bool operator ==(Entity a, Entity b) 
    {
        return a is not null && b is not null && a.Id == b.Id;
    }
    public static bool operator !=(Entity a, Entity b) => !(a == b);

    public override int GetHashCode() => HashCode.Combine(GetEntityType().Id, Id);
    public Entity.Type GetEntityType()
    {
        return TValues.Type;
    }
    public uint GetEntityId()
    {
        return Id;
    }

    public void Destroy()
    {
        if (isDestroyed)
        {
            Main.LOGGER.Wrn($"Entity {this} is already destroyed!");
            return;
        }
        Entities.Destroy(this);
    }

    public bool IsDestroyed() => isDestroyed;

    public readonly struct Type
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
            if(EntityTypeRegistry.TryGet(this, out System.Type classType))
                return $"Entity Class[{classType.Name}]";

            throw new NullReferenceException($"Entity.Type references Null Entity.Type with Id[{Id}]");
        }
    }
    public readonly struct Tag(ushort tagId)
    {
        public readonly ushort Id = tagId;

        public static bool operator ==(Entity.Tag a, Entity.Tag b) => a.Id == b.Id;
        public static bool operator !=(Entity.Tag a, Entity.Tag b) => a.Id != b.Id;

        public bool Equals(Entity.Tag other) => Id == other.Id;

        public override bool Equals(object obj) => obj is Entity.Tag other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString()
        {
            if(EntityTagRegistry.TryGet(this, out string tag))
                return tag;

            throw new NullReferenceException($"Tag references Null Tag with Id[{Id}]");
        }
    }
    public class TypeValues(Entity.Type type)
    {
        public readonly Entity.Type Type = type;

        public Vector2 StartingScale { private set; get; } = Vector2.One;
        internal void SetStartingScale(Vector2 scale)
        {
            StartingScale = scale;
        }

        internal void Init()
        {
            Setup();
        }

        protected virtual void Setup() { }

        public HashSet<Tag> Tags { private set; get; } = [];
        public void SetTags(HashSet<Tag> tags) 
        { 
            Tags.Clear();
            Tags.UnionWith(tags);
        }
        public bool AddTag(Tag tag) => EntityTagRegistry.TagExists(tag) && Tags.Add(tag);
        public bool AddTag(string tagString) => EntityTagRegistry.TryGet(tagString, out Tag tag) && Tags.Add(tag);
        public bool HasTag(Tag tag) => Tags.Contains(tag);
        public bool HasTag(string tagString) => EntityTagRegistry.TryGet(tagString, out Tag tag) && HasTag(tag);
        public bool RemTag(Tag tag) => Tags.Remove(tag);
        public bool RemTag(string tagString) => EntityTagRegistry.TryGet(tagString, out Tag tag) && RemTag(tag);
        public void ClrTags() => Tags.Clear();
    } 
}
