using System;
using System.Collections.Generic;
using Luminix.RegistryBuilders;
using Luminix.Workspaces;

namespace Luminix;

public sealed class TypeRegistry : Workspace.Service
{
    private bool finalized;
    private Dictionary<string, TileTypeBuilder> tileTypeBuilders = new();

    public static TBuilder NewTileBuilder<TBuilder>(string name) where TBuilder : TileTypeBuilder
    {
        return (TBuilder)Activator.CreateInstance(typeof(TBuilder), name)!;
    }
    public void Register(TileTypeBuilder tileTypeBuilder)
    {
        if(tileTypeBuilders.ContainsKey(tileTypeBuilder.TypeName))
            throw new InvalidOperationException($"Duplicate TileType registration: A builder for '{tileTypeBuilder.TypeName}' already exists in the registry.");

        tileTypeBuilders.Add(tileTypeBuilder.TypeName, tileTypeBuilder);
    }

    public bool TryGetBuilder<TBuilder>(string registryName, out TBuilder _builder) where TBuilder : TileTypeBuilder
    {
        _builder = null;

        if(finalized)
        {
            Logger.Get().Wrn($"Cannot retrieve TileTypeBuilder '{registryName}' because the TileType registry has already been finalized.");
            return false;
        }

        if (tileTypeBuilders.TryGetValue(registryName, out var baseBuilder))
        {
            if (baseBuilder is TBuilder typedBuilder)
            {
                _builder = typedBuilder;
                return true;
            }

            Logger.Get().Err($"TileType '{registryName}' found, but is not of type {typeof(TBuilder).Name}.");
        }

        return false;
    }

    internal void FinalizeRegistry()
    {
        tileTypeBuilders = null;

        foreach(var builder in tileTypeBuilders)
        {
            
        }

        finalized = true;
    }
}
