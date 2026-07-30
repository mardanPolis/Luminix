using System;
using System.Reflection;
using System.Text.Json;
using Luminix.RegistrySystem;

namespace Luminix.Core.Handlers;

internal interface IDataHandler
{
    void Parse(string data);
}

internal class DataHandler<TEntry> : IDataHandler where TEntry : EntryItem<TEntry>
{
    public DataHandler()
    {
        entryType = typeof(TEntry);
        if(entryType.IsAbstract)
            throw new InvalidOperationException();
        
        entryTypeCtor = entryType.GetConstructor(new Type[] {typeof(Registry<TEntry>.Token), typeof(string)});
        
        registry = Workspace.Current.GetService<RegistryManager>().GetRegistry<TEntry>();
    }

    private Registry<TEntry> registry;
    private Type entryType;
    private ConstructorInfo entryTypeCtor;


    public void Parse(string data)
    {
        using JsonDocument doc = JsonDocument.Parse(data);

        foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
        {
            string entryId = entry.Name;

            JsonElement snapshot = entry.Value.Clone();

            registry.Register(entryId, (token, id) =>
            {
                
            });
        }
    }
}