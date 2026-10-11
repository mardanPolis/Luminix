using Luminix;
using Microsoft.Xna.Framework;

namespace Sandbox;

public class SandboxBehavior : ModuleBehavior
{
    private Defs defs;
    private World world;

    protected override void Initialize()
    {
        defs = Workspace.GetService<Defs>();
        world = Workspace.GetService<World>();
    }

    protected override void Start()
    {
        var myEntityType = defs.GetDef<EntityType>("MyEntity");
        var entity1 = world.CreateEntity(myEntityType);
        ref var transform = ref world.GetEntityComponent<TransformInterp>(entity1);
        transform = new Transform(new Vector3(1, 1, 0), new Vector2(1, 1), 0);
    }
}