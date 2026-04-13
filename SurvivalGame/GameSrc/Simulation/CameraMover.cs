using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Verse;
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

        if(Input.GetKeyDown(Keys.W))
            cameraPos.Y += speed * (camera.OrthographicSize * 0.1f);
        if(Input.GetKeyDown(Keys.S))
            cameraPos.Y -= speed * (camera.OrthographicSize * 0.1f);

        if(Input.GetKeyDown(Keys.A))
            cameraPos.X -= speed * (camera.OrthographicSize * 0.1f);
        if(Input.GetKeyDown(Keys.D))
            cameraPos.X += speed * (camera.OrthographicSize * 0.1f);
            
        float direction = 0;
        if(Input.GetKeyDown(Keys.E)) direction += 1f;
        if(Input.GetKeyDown(Keys.Q)) direction -= 1f;

        if (direction != 0)
        {
            float targetSize = camera.OrthographicSize + (direction * speed * 10f);
            camera.ScaleTo(targetSize);
        }

        camera.MoveTo(cameraPos);
    }
}
