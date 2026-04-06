using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Verse.Rendering;
using Verse.Simulation;

namespace SurvivalGame.GameSrc.Simulation;

public class CameraMover : WorldComponent
{
    private Camera camera;
    public override void Initialize()
    {
        camera = new Camera();
    }

    public override void Tick()
    {
        float speed = (float)(5f * World.TickDeltaTime);

        var cameraPos = camera.Position;

        KeyboardState state = Keyboard.GetState();

        if(state.IsKeyDown(Keys.W))
            cameraPos.Y += speed;
        if(state.IsKeyDown(Keys.S))
            cameraPos.Y -= speed;

        if(state.IsKeyDown(Keys.A))
            cameraPos.X -= speed;
        if(state.IsKeyDown(Keys.D))
            cameraPos.X += speed;

        if(state.IsKeyDown(Keys.Q))
            camera.ScaleTo(camera.OrthographicSize - speed * 5f);
        if(state.IsKeyDown(Keys.E))
            camera.ScaleTo(camera.OrthographicSize + speed * 5f);

        camera.MoveTo(cameraPos);
    }
}
