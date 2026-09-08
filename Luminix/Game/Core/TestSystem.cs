using System;

namespace Luminix.Game;

public class TestSystem : EntitySystem
{
    public float MaxHealth { get; set; }
    public float MaxStamina { get; set; }

    public TextureResource triangle;

    public override string ToString()
    {
        return $"TestSystem, [MaxHealth: {MaxHealth}, MaxStamina: {MaxStamina}]";
    }

    private void Update(ref Transform transform)
    {
        var time = Time.TickCount;
        transform.Position.X = MathF.Sin((float)time);
    }

    private void Render(in Transform transform)
    {
        Graphics.DrawSprite(triangle, transform);
    }
}
