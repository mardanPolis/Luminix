using System;
using Microsoft.Xna.Framework.Input;
using Luminix;
using Luminix.Simulation;

namespace Game.Simulation;

public class CameraMover : WorldComponent
{
    private Input input;

    public override void Initialize()
    {
        Workspace.TryGetService(out input);
    }

    public override void Tick(Map currentMap)
    {
        Camera camera = currentMap.Camera;
        float speed = (float)(5f * Universe.DeltaTime);

        var cameraPos = camera.Position;

        if(input.IsKeyDown(Keys.W))
            cameraPos.Y += speed * (camera.OrthographicSize * 0.1f);
        if(input.IsKeyDown(Keys.S))
            cameraPos.Y -= speed * (camera.OrthographicSize * 0.1f);

        if(input.IsKeyDown(Keys.A))
            cameraPos.X -= speed * (camera.OrthographicSize * 0.1f);
        if(input.IsKeyDown(Keys.D))
            cameraPos.X += speed * (camera.OrthographicSize * 0.1f);
            
        float direction = 0;
        if(input.IsKeyDown(Keys.E)) direction += 1f;
        if(input.IsKeyDown(Keys.Q)) direction -= 1f;

        if (direction != 0)
        {
            float targetSize = camera.OrthographicSize + (direction * speed * 10f);
            targetSize = Math.Clamp(targetSize, 5, 150);
            camera.ScaleTo(targetSize);
        }

        camera.MoveTo(cameraPos);
    }
}
