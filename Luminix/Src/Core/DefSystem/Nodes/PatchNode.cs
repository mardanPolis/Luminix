namespace Luminix;

public abstract class PatchNode
{
    public required string DefID { get; init; }

    public abstract void Patch(DefNode def);
}