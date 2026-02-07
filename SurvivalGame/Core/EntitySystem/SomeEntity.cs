using System;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public class SomeEntity : DynamicEntity
{ 
    ~SomeEntity()
    {
        SurvivalGame.LOGGER.Log($"{Id} got destroyed!");
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
        SurvivalGame.LOGGER.Log($"{Id} got destroyed!");
    }

    public void Speak()
    {
        SurvivalGame.LOGGER.Log("HELLO WORLD! FROM SOMEENTITY!");
    }

    public override string ToString()
    {
        return base.ToString() + $", random: {randomValue}";
    }
}
