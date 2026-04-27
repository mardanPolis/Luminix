using System;
using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderItems;

public struct RenderItemSpriteBatch
{
    public RenderItemSpriteBatch(Sprite sprite, float layerDepth)
    {
        Sprite = sprite;
        LayerDepth = layerDepth;
    }

    public Sprite Sprite = null;
    public float LayerDepth;
    public int StartIndex;
    public int Length;
    public Vector2 Scale;
}
