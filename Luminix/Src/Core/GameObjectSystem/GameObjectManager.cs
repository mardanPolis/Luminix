using System;
using System.Collections.Generic;

namespace Luminix;

public sealed class GameObjectManager : Workspace.Service
{
    internal readonly struct Controller 
    {
        internal Controller(GameObjectManager controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a World.Controller, the provided universe is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly GameObjectManager controlled;

        public void UpdateGameObjects()
        {
            foreach(var gameObject in controlled.gameObjects.Values)
            {
                gameObject.Update();
            }
        }

        public void RenderGameObjects()
        {
            foreach(var gameObject in controlled.gameObjects.Values)
            {
                gameObject.Render();
            }
        }

    }
    internal GameObjectManager(out Controller controller)
    {
        controller = new Controller(this);
        gameObjects = new();
    }

    private bool controllerIssued;
    private Dictionary<Type, GameObject> gameObjects;

    public void Add(GameObject gameObject)
    {
        gameObjects.Add(gameObject.GetType(), gameObject);
    }
    public bool TryAdd(GameObject gameObject)
    {
        return gameObjects.TryAdd(gameObject.GetType(), gameObject);
    }

    public bool TryGetGameObject<T>(out T gameObject) where T : GameObject
    {
        if (gameObjects.TryGetValue(typeof(T), out var go)) {
            gameObject = (T)go;
            return true;
        }
        gameObject = null;
        return false;
    }

    public void Remove(GameObject gameObject) {
        gameObjects.Remove(gameObject.GetType());
    }

}