using Microsoft.Xna.Framework;

namespace Luminix.Rendering.RenderItems;

internal struct RenderItemText
{
    public RenderItemText(string text, Vector2 position, Vector2 scale, float rotation, bool bold, Color color, float layerDepth)
    {
        Text = text;
        Position = position;
        Scale = scale;
        Rotation = rotation;
        Bold = bold;
        Color = color;
        LayerDepth = layerDepth;
    }
    public string Text;
    public Vector2 Position;
    public Vector2 Scale;
    public float Rotation;
    public bool Bold;
    public Color Color;
    public float LayerDepth;
}
