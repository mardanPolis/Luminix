using Microsoft.Xna.Framework;

namespace Verse.Rendering;
internal struct InstanceLine
{
    public InstanceLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
    }

    public Vector2 Start;
    public Vector2 End;
    public Color Color;
    public float Thickness;
}