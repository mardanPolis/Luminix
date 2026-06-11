namespace Luminix;

internal interface IComponentContainer
{
    
}
internal sealed class ComponentContainer<T> : IComponentContainer
{

    public T GetComponent()
    {
        throw new System.NotImplementedException();
    }
}