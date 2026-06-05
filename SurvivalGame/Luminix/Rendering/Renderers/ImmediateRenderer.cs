using Luminix.Resources;
using Luminix.Workspaces;

namespace Luminix.Rendering;

public abstract class ImmediateRenderer
{
    public ImmediateRenderer()
    {

        resourceProvider = Workspace.Current.GetService<RenderingSystem>().ResourceManager.ResourceProvider;
    }

    protected readonly ResourceProvider resourceProvider;
    public abstract void Render();

}