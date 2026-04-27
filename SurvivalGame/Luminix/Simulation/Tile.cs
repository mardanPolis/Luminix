using System;
using System.Collections.Generic;
using Luminix.Identifiers;

namespace SurvivalGame.Verse.Simulation;

public sealed class Tile(string typeName, ID textureID, short movabilityCost, short supportLevel, Dictionary<string, int> intVariables, Dictionary<string, Tag> tags)
{
    public readonly string TypeName = typeName;
    public readonly ID TextureID = textureID;

    public readonly short MovabilityCost = movabilityCost;
    public readonly short SupportLevel = supportLevel;

    public readonly Dictionary<string, int> IntVariables = intVariables;
    public readonly Dictionary<string, Tag> Tags = tags;
}
