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
        float speed = 0.1f;

        var cameraPos = camera.Position;

        if(Keyboard.GetState().IsKeyDown(Keys.W))
            cameraPos.Y += speed;
        if(Keyboard.GetState().IsKeyDown(Keys.S))
            cameraPos.Y -= speed;

        if(Keyboard.GetState().IsKeyDown(Keys.A))
            cameraPos.X -= speed;
        if(Keyboard.GetState().IsKeyDown(Keys.D))
            cameraPos.X += speed;

        if(Keyboard.GetState().IsKeyDown(Keys.Q))
            camera.ScaleTo(camera.OrthographicSize - speed * 5f);
        if(Keyboard.GetState().IsKeyDown(Keys.E))
            camera.ScaleTo(camera.OrthographicSize + speed * 5f);

        camera.MoveTo(cameraPos);
    }
}
