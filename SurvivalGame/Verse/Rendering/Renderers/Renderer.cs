using System;
using System.Collections;
using Verse.Collections;
using Verse.Identifiers;

namespace Verse.Rendering;

public abstract class Renderer
{
    public abstract void RenderOn(RenderStack renderStack);
    internal abstract void Assign(ICollection list, Type renderableType);
    internal abstract void Optimize();
}

public abstract class Renderer<T> : Renderer
{
    private DenseList<T>[] renderables;
    private int count = 0;

    internal override void Assign(ICollection renderables, Type renderableType)
    {
        if(renderables is not DenseList<T> typedRenderables)
            throw new ArgumentException($"Expected a DenseList<{typeof(T).FullName}> but got {renderables.GetType().FullName}.");

        if (this.renderables == null || count == this.renderables.Length)
        {
            var newRenderables = new DenseList<T>[this.renderables.Length + 2];
            Array.Copy(this.renderables, newRenderables, count);
            this.renderables = newRenderables;
        }

        this.renderables[count] = (DenseList<T>)renderables;
        count++;
    }

    internal override void Optimize()
    {
        // resize renderables array to fit count
        if(renderables == null || count == renderables.Length)
            return;

        var newRenderables = new DenseList<T>[count];
        Array.Copy(this.renderables, newRenderables, count);
        this.renderables = newRenderables; 
    }


    public override void RenderOn(RenderStack renderStack)
    {
        
    }

    public abstract void Render(T renderable, RenderStack renderStack);
}
