using System;
using System.Collections.Generic;
using SurvivalGame.Verse.Simulation;
using Luminix.Identifiers;
using Luminix.Resources;

namespace Luminix.Core.RegistryBuilders;

public class TileTypeBuilder
{
    public TileTypeBuilder(string name)
    {
        TypeName = name;
    }

    public readonly string TypeName;
    public string Texture;
    public short TypeMovabilityCost { get; protected set; }
    public short TypeSupportLevel { get; protected set; }

    public Dictionary<string, int> IntVariables = new();
    public Dictionary<string, Tag> Tags = new();

    // public TileTypeBuilder Textured(string texture)
    // {
    //     try
    //     {
    //         var texture2D = ContentManager.LoadTexture(texture);
    //         if(texture2D.Width != Settings.PixelsPerUnit || texture2D.Height != Settings.PixelsPerUnit)
    //             throw new ArgumentException($"Texture '{texture}' can't be a tile texture because its size is not {Settings.PixelsPerUnit}x{Settings.PixelsPerUnit}");
            
    //         Texture = texture;
    //     }
    //     catch(Exception ex)
    //     {
    //         Logger.Get().Wrn($"Failed to assign texture '{texture}' to TileType, exception: {ex}");
    //         Texture = "";
    //     }

    //     return this;
    // }
    
    public TileTypeBuilder Movability(short cost = 1)
    {
        if(cost == 0)
        {
            Logger.Get().Wrn("Tile MovabilityCost cant be 0, defaulting MovabilityCost to '1'!");
            cost = 1;
        }
        
        TypeMovabilityCost = cost;
        return this;
    }


    public TileTypeBuilder SupportLevel(short level = 1)
    {
        TypeSupportLevel = level;
        return this;
    }

    public TileTypeBuilder Valued(string varName, int value)
    {
        IntVariables.Add(varName, value);
        return this;
    }
    public TileTypeBuilder Tagged(string tag)
    {
        return this;
        throw new InvalidOperationException($"Tag {tag} does not exist!");
    }

    public Tile Build(Dictionary<string, ID> tileTextures)
    {
        return new Tile(TypeName, tileTextures[Texture], TypeMovabilityCost, TypeSupportLevel, IntVariables, Tags);
    }
}
