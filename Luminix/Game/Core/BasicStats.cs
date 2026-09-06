namespace Luminix.Game;

public struct BasicStats
{
    public float Health;
    public float Stamina;

    public override string ToString()
    {
        return $"Health: [{Health}], Stamina: [{Stamina}]";
    }
}