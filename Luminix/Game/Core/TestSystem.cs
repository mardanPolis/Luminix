using System;
using Luminix;

namespace Luminix.Game;

public class TestSystem : EntitySystem
{
    public float MaxHealth { get; set; }
    public float MaxStamina { get; set; }

    public override string ToString()
    {
        return $"TestSystem, [MaxHealth: {MaxHealth}, MaxStamina: {MaxStamina}]";
    }

    private void Render()
    {
    }
}
