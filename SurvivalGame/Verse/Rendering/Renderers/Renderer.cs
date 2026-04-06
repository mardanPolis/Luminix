using System;
using System.Collections;
using System.Collections.Generic;
using Verse.Collections;

namespace Verse.Rendering;

public abstract class Renderer
{
    public Type RenderableType { get; protected internal init; }
    public abstract void Initialize();
    public abstract void RenderOn(RenderStack renderStack);
    public abstract void RenderGizmosOn(RenderStack renderStack);
    internal abstract void Assign(IEnumerable list);
    internal abstract void Optimize();
}

public abstract class Renderer<T> : Renderer
{
    public Renderer()
    {
        RenderableType = typeof(T);
    }

    internal interface IFastRenderable<TBase>
    {
        void ExecuteRender(Renderer<TBase> renderer, RenderStack stack);
        void ExecuteGizmosRender(Renderer<TBase> renderer, RenderStack stack);
    }

    private sealed class DenseListWrapper<TSub, TBase> : IFastRenderable<TBase> 
        where TSub : TBase
    {
        private readonly DenseList<TSub> _list;

        public DenseListWrapper(DenseList<TSub> list) => _list = list;

        public void ExecuteRender(Renderer<TBase> renderer, RenderStack stack)
        {
            for (int i = 0; i < _list.Count; i++)
                renderer.Render(_list[i], stack); 
        }
        public void ExecuteGizmosRender(Renderer<TBase> renderer, RenderStack stack)
        {
            for (int i = 0; i < _list.Count; i++)
                renderer.RenderGizmo(_list[i], stack); 
        }
    }

    private IFastRenderable<T>[] renderables = [];
    private int count = 0;

    public sealed override void RenderOn(RenderStack renderStack)
    {
        for (int i = 0; i < renderables.Length; i++)
            renderables[i].ExecuteRender(this, renderStack);
    }
    public sealed override void RenderGizmosOn(RenderStack renderStack)
    {
        for (int i = 0; i < renderables.Length; i++)
            renderables[i].ExecuteGizmosRender(this, renderStack);
    }

    internal sealed override void Assign(IEnumerable list)
    {
        var listType = list.GetType();
        
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(DenseList<>))
        {
            var itemType = listType.GetGenericArguments()[0];
            
            if (typeof(T).IsAssignableFrom(itemType))
            {
                var method = typeof(Renderer<T>).GetMethod("AssignSub", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var genericMethod = method.MakeGenericMethod(itemType);
                genericMethod.Invoke(this, new object[] { list });
            }
        }
    }
    private void AssignSub<TSub>(DenseList<TSub> list) where TSub : T
    {
       ArgumentNullException.ThrowIfNull(list, nameof(list));

        // Fix: If the array is empty, give it a size of 4. Otherwise, double it.
        if (count >= renderables.Length)
        {
            int newSize = renderables.Length == 0 ? 4 : renderables.Length * 2;
            Array.Resize(ref renderables, newSize);
        }

        renderables[count] = new DenseListWrapper<TSub, T>(list);
        count++;
    }

    internal sealed override void Optimize()
    {
        if (renderables == null || count == renderables.Length)
            return;

        var trimmedRenderables = new IFastRenderable<T>[count];
        
        Array.Copy(this.renderables, trimmedRenderables, count);
        
        this.renderables = trimmedRenderables;
    }

    public abstract void Render(T renderable, RenderStack renderStack);
    public virtual void RenderGizmo(T renderable, RenderStack renderStack) {}
}

public abstract class GlobalRenderer : Renderer
{
    public GlobalRenderer()
    {
        RenderableType = null;
    }

    internal sealed override void Assign(IEnumerable list)
    {
        Console.WriteLine($"Cant assign '{list.GetType().Name}' to this '{this.GetType().Name}'! Reason: GlobalRenderer");
    }

    internal sealed override void Optimize()
    {
        Console.WriteLine($"Cant optimize '{this.GetType().Name}'! Reason: GlobalRenderer");
    }
}