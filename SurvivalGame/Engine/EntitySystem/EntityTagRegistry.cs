using System;
using System.Collections.Generic;
using System.Linq;

namespace SurvivalGame.Engine.EntitySystem;

public static class EntityTagRegistry
{
    private sealed class InitData
    {
        public readonly HashSet<string> seenEntityTags = [];
        public readonly List<string> entityTags = [];
    }

    private readonly struct RegistryData(Dictionary<string, Entity.Tag> Tags)
    {
        public readonly Dictionary<string, Entity.Tag> tags = Tags;
        public readonly string[] tagsStrings = Tags.Keys.ToArray();
        public readonly ushort EntityTagCount => (ushort)tags.Count;
    }

    private static InitData initData = new();
    private static RegistryData registryData;

    public static void RegisterTag(string tag)
    {
        tag = tag.ToLower();

        if(IsFinalized())
            throw new InvalidOperationException($"Tried to register tag '{tag}', but Entity registry already finalized!");

        if(initData.entityTags.Count >= ushort.MaxValue)
            throw new ArgumentException($"Tried to register Entity.Tag '{tag}', but Entity Tag registry has reached max registry count - {ushort.MaxValue}");

        if(initData.seenEntityTags.Contains(tag))
        {
            Main.LOGGER.Wrn($"Entity.Tag '{tag}' already registered");
            return;
        }

        initData.entityTags.Add(tag);
        initData.seenEntityTags.Add(tag);
    }
    
    public static void FinalizeRegistry()
    {
        Dictionary<string, Entity.Tag> tags = new();
        
        List<string> entityTags = initData.entityTags;
        for(ushort i = 0; i < entityTags.Count; i++)
        {
            var tag = entityTags[i];
            if(tags.ContainsKey(tag))
            {
                Main.LOGGER.Wrn($"Entity.Tag '{tag}' already registered");
                continue;
            }
            tags.Add(tag, new Entity.Tag(i));
        }

        registryData = new(tags);
        initData = null;
        

        Main.LOGGER.Log($"Registered Tags ({registryData.tagsStrings.Length}):");
        foreach(var entry in tags)
            Main.LOGGER.Log($"- id:{entry.Value.Id} -> {entry.Key}");
    }

    public static bool IsFinalized()
    {
        return initData == null;
    }

    public static bool TagExists(string registryTag)
    {
        return registryData.tags.ContainsKey(registryTag);
    }
    public static bool TagExists(Entity.Tag tag)
    {
        return tag.Id < registryData.EntityTagCount;
    }
    public static bool TryGet(string registryTag, out Entity.Tag tag)
    {
        return registryData.tags.TryGetValue(registryTag, out tag);
    }
    public static bool TryGet(Entity.Tag tag, out string registryTag)
    {
        registryTag = string.Empty;

        if(registryData.tagsStrings.Length <= tag.Id)
            return false;

        registryTag = registryData.tagsStrings[tag.Id];
        return true;
    }
}
