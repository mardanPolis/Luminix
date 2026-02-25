using System;
using Microsoft.Xna.Framework;
using SurvivalGame.Engine.Rendering;

namespace SurvivalGame.Engine.EntitySystem;

public class SomeEntity : DynamicEntity
{ 
    ~SomeEntity()
    {
        Main.LOGGER.Log($"{Id} got destroyed!");
    }    
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

    public override void OnDestroy()
    {
        Main.LOGGER.Log($"{Id} got destroyed!");
    }

    public void Speak()
    {
        Main.LOGGER.Log("HELLO WORLD! FROM SOMEENTITY!");
    }

    public override string ToString()
    {
        return base.ToString() + $", random: {randomValue}";
    }
}
