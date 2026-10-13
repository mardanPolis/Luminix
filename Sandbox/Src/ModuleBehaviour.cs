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
        //Gets the MyEntity EntityType
        var myEntityType = defs.GetDef<EntityType>("MyEntity");

        //Creates a instance of MyEntity
        var entity1 = world.CreateEntity(myEntityType);

        //Gets the created MyEntity instance Interpolated Transform and sets its starting position.
        ref var transform = ref world.GetEntityComponent<TransformInterp>(entity1);
        transform = new Transform(new Vector3(1, 1, 0));

        for(int i = 0; i < 1_000_000; i++)
        {
            world.CreateEntity(myEntityType);
        }
    }
}