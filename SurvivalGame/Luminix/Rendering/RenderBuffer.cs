using System;
using Microsoft.Xna.Framework;
using Luminix.Simulation;
using Luminix.Identifiers;
using System.Collections.Generic;

namespace Luminix.Rendering;

public sealed class RenderBuffer(ID iD)
{
    public readonly ID stackID = iD;

}

