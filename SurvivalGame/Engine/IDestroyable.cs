using System;

namespace SurvivalGame.Engine;

public interface IDestroyable
{
    abstract void OnDestroy();
    bool IsDestroyed();
    void Destroy();
}
