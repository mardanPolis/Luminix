using System;
using Luminix.Collections;

namespace Luminix;

internal interface IComponentType
{
    public Type Type { get; }
    public int Size { get; }
    public object Default { get; set; }
    public IComponentContainer GetComponentContainer();
}