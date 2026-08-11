using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Luminix.Core.Exceptions;

namespace Luminix.RegistrySystem;

public interface IRegistry
{
    public Type EntryType { get; }
    public bool IsFrozen { get; }
    public bool EntryExists(string entryID);
    public string ToString();

    internal void Freeze();
}

public sealed class Registry<T> : IRegistry where T : EntryItem<T>
{
    public sealed class Token();

    internal Registry()
    {
        entries = new Dictionary<string, T>();
    }

    public Type EntryType => typeof(T);
    public bool IsFrozen => entries is FrozenDictionary<string, T>;

    public T[] Entries => entries.Values.ToArray();

    private IDictionary<string, T> entries;

    private Token cachedToken = null;

    public void Register(string entryID, Func<Token, string, T> factory)
    {
        try
        {
            if(IsFrozen)
                throw new InvalidOperationException($"Registry is frozen!");

            if(string.IsNullOrWhiteSpace(entryID))
                throw new ArgumentException($"Provided type name is null, empty or consists only of white-space characters!");

            entryID = entryID.ToLower();
            
            if(entries.ContainsKey(entryID))
                throw new ArgumentException($"Entry with the same name already exists!");

            var token = new Token();
            var entry = factory(token, entryID);
            
            entries[entryID] = entry;

        }
        catch(Exception ex)
        {
            Logger.Get().Err($"Unable to register '{entryID}', reason: {ex.Message}");
        }
    }

    public bool EntryExists(string entryID)
    {
        return entries.ContainsKey(entryID); 
    }

    public T GetEntry(string entryID)
    {
        if (entries.TryGetValue(entryID, out T entry))
            return entry;

        throw new KeyNotFoundException($"No Entry registered with name: {entryID}");
    }

    public sealed override string ToString()
    {
        return $"{EntryType.Name} Registry";
    }

    internal void ValidateToken(Token token)
    {
        if (!cachedToken.Equals(token))
            throw new InvalidTokenException();

        cachedToken = null;
    }

    void IRegistry.Freeze()
    {
        if (IsFrozen) return;
        
        entries = entries.ToFrozenDictionary();
    }
}