using System;
using System.Collections;
using System.Collections.Generic;
using Verse.Collections;
using Verse.Identifiers;

namespace Verse.Rendering;

public abstract class Renderer
{
    public abstract void RenderOn(RenderStack renderStack);
    public abstract void RenderGizmosOn(RenderStack renderStack);
    internal abstract void Assign(IEnumerable list);
    internal abstract void Optimize();
    public Type RenderableType { get; protected internal set; }
}

public abstract class Renderer<T> : Renderer
{
    public Renderer()
    {
        RenderableType = typeof(T);
    }

    private IEnumerable<T>[] renderables;
    private int count = 0;

    internal sealed override void Assign(IEnumerable list)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));

        // Only allow collections that already implement IEnumerable<T>
        if (list is not IEnumerable<T> typedEnumerable)
        {
            throw new InvalidOperationException(
                $"Renderer<{typeof(T)}> can only accept IEnumerable<{typeof(T)}> collections.");
        }

        if (this.renderables == null || count == this.renderables.Length)
        {
            var newRenderables = new IEnumerable<T>[this.renderables.Length + 2];
            Array.Copy(this.renderables, newRenderables, count);
            this.renderables = newRenderables;
        }

        this.renderables[count] = typedEnumerable;
        count++;
    }

    internal sealed override void Optimize()
    {
        // resize renderables array to fit count
        if(renderables == null || count == renderables.Length)
            return;

        var newRenderables = new IEnumerable<T>[count];
        Array.Copy(this.renderables, newRenderables, count);
        this.renderables = newRenderables; 
    }


    public sealed override void RenderOn(RenderStack renderStack)
    {
        foreach (var collection in renderables)
        {
            foreach (T renderable in collection)
            {
                Render(renderable, renderStack);
            }
        }
    }

    public sealed override void RenderGizmosOn(RenderStack renderStack)
    {
        foreach (var collection in renderables)
        {
            foreach (T renderable in collection)
            {
                RenderGizmo(renderable, renderStack);
            }
        }
    }

    public abstract void Render(T renderable, RenderStack renderStack);
    public abstract void RenderGizmo(T renderable, RenderStack renderStack);
}
