namespace Luminix;

public abstract class Def
{
    public Def(Defs.Token token, string name)
    {
        Workspace.Current.GetService<Defs>().ValidateToken(token);
        Name = name;
    }

    protected virtual void Initialize() {}

    public required string Name { get; init; }
}