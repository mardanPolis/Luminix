namespace Luminix.Threading.Commands;

internal struct ResourceCommand
{
    public enum Type
    {
        create,
        update,
        unload,
    }

    public Type Kind;
    public string Identifier;
    public Asset Asset;
}