using System;
using System.Reflection;
using System.Text.Json;
using Luminix.IO;
using Luminix.RegistrySystem;

namespace Luminix.Core.Handlers;

internal interface IDataHandler
{
    void Parse(DataAsset data);
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


    public void Parse(DataAsset data)
    {
    }
}