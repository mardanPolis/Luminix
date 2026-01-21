using System;

namespace SurvivalGame.Core.EntitySystem;

public class SomeEntity : DynamicEntity
{
    public SomeEntity(uint id, TypeValues typeValues) : base(id, typeValues) { }
    public int randomValue;

    public override void Awake()
    {
        Random random = new();
        randomValue = random.Next();
    }

    public override void Start()
    {
    }

    public override void PostTick()
    {
    }

    public override void PreTick()
    {
    }

    public override void Tick()
    {
    }

    public override string ToString()
    {
        return base.ToString() + $", random: {randomValue}";
    }
}
