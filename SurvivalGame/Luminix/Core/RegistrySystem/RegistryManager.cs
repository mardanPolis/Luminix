using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Luminix.RegistrySystem;

public sealed class RegistryManager : Workspace.Service
{
    public RegistryManager()
    {
        registries = new Dictionary<Type, IRegistry>();
    }

    public bool IsFrozen => registries is FrozenDictionary<Type, IRegistry>;

    private IDictionary<Type, IRegistry> registries;

    public void NewRegistry<TEntry>() where TEntry : EntryItem<TEntry>
    {
        try
        {
            if(IsFrozen)
                throw new InvalidOperationException($"RegistryManager is frozen!");

            var registryEntryType = typeof(TEntry);
            if(registries.ContainsKey(registryEntryType))
                throw new InvalidOperationException($"Re");

            registries[registryEntryType] = new Registry<TEntry>();
        }
        catch(Exception ex)
        {
            Logger.Get().Err($"Unable to register 'Registry<{typeof(TEntry).Name}>', reason: {ex.Message}");
        }
    }

    public Registry<TEntry> GetRegistry<TEntry>() where TEntry : EntryItem<TEntry>
    {
        if(registries.TryGetValue(typeof(TEntry), out IRegistry registry))
            return (Registry<TEntry>)registry;
        
        throw new KeyNotFoundException($"No 'Registry<{typeof(TEntry).Name}>' found!");
    }

    internal void Freeze()
    {
        if(IsFrozen) return;

        foreach(var registry in registries.Values)
            registry.Freeze();

        registries = registries.ToFrozenDictionary();
    }
}