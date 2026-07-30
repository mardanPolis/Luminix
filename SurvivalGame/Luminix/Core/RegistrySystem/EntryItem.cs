namespace Luminix.RegistrySystem;

public abstract class EntryItem<T> where T : EntryItem<T>
{
    public EntryItem(Registry<T>.Token token, string id)
    {
        Workspace.Current.GetService<RegistryManager>().GetRegistry<T>().ValidateToken(token);
        ID = id;
    }

    public string ID { get; }
}