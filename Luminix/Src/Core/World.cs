using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Luminix.Collections;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class World : Workspace.Service
{
    internal readonly struct Controller 
    {
        static Controller()
        {
            var updateMethodInfo = typeof(GameObject).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var renderMethodInfo = typeof(GameObject).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance);

            updateMethod = (Action<GameObject>)Delegate.CreateDelegate(typeof(Action<GameObject>), updateMethodInfo);
            renderMethod = (Action<GameObject>)Delegate.CreateDelegate(typeof(Action<GameObject>), renderMethodInfo);
        }

        internal Controller(World controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a World.Controller, the provided universe is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly World controlled;

        private readonly static Action<GameObject> updateMethod;
        private readonly static Action<GameObject> renderMethod;

        public readonly void Tick()
        {
            foreach(var gameObject in controlled.gameObjects.Values)
            {
                try
                {
                    updateMethod(gameObject);
                }
                catch(Exception ex)
                {
                    Logger.NewLog(Logger.Log.Level.Error, $"Failed to Update GameObject {gameObject}, reason: {ex}");
                }
            }

            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].Tick();
            }
        }

        public readonly void StartCreatedEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].StartEntities();
            }
        }

        public readonly void PrepareEntities()
        {
            controlled.isPreparing = true;
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].PrepareEntities();
            }
            controlled.isPreparing = false;
        }

        public readonly void UpdateEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].UpdateEntities();
            }
        }

        public readonly void RenderEntities()
        {
            foreach(var gameObject in controlled.gameObjects.Values)
            {
                try
                {
                    renderMethod(gameObject);
                }
                catch(Exception ex)
                {
                    Logger.NewLog(Logger.Log.Level.Error, $"Failed to Render GameObject {gameObject}, reason: {ex}");
                }
            }

            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].RenderEntities();
            }
        }

        public readonly void DestroyEntities()
        {
            var containers = controlled.entityContainers;
            for(int i = 0; i < containers.Length; i++)
            {
                containers[i].DestroyEntities();
            }
        }
    }

    internal World(Graphics graphics, out Controller controller)
    {
        controller = new Controller(this);
        this.graphics = graphics;
        this.controllerIssued = true;
        this.gameObjects = new();

        taggedEntities = new();
        entityTags = new();
        listPool = ObjectPool<List<string>>.Create((list) => list.Clear(), 64);
    }

    private Graphics graphics;

    private readonly bool controllerIssued;

    private EntityID entityID;

    private Dictionary<Type, GameObject> gameObjects;
    private Dictionary<EntityID, EntityType> entities;

    private Dictionary<string, DenseList<EntityID>> taggedEntities;
    private Dictionary<EntityID, List<string>> entityTags;
    private ObjectPool<List<string>> listPool;

    private Dictionary<EntityType, EntityContainer> typeContainers;
    private EntityContainer[] entityContainers;

    private bool isInitialized;

    private bool isPreparing;

    protected override void Initialize()
    {
        Logger.NewLog(Logger.Log.Level.Info, "Initializing World");

        var defs = Workspace.GetService<Defs>();
        var entityTypes = defs.GetDefs<EntityType>();
        entityContainers = CreateEntityContainers(entityTypes);
        entities = [];
        typeContainers = [];
        
        foreach(EntityContainer container in entityContainers)
            typeContainers[container.EntityType] = container;

        foreach(EntityContainer container in entityContainers)
            container.EntitySystem?.Initialize(container.EntityType);
        
        isInitialized = true;
    }

    protected override void Start()
    {
        var camera = Workspace.GetService<Defs>().GetDef<EntityType>("camera");
        var cameraID = CreateEntity(camera);
        graphics.MainCamera = cameraID;

        ref Camera cameraData = ref GetEntityComponent<Camera>(cameraID);
        cameraData.BackgroundColor = new Color(49, 77, 121);
    }

    //Entity Lifetime
    public EntityID CreateEntity(EntityType entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType, nameof(entityType));

        if(!isInitialized)
            throw new InvalidOperationException("Cant create an Entity when the world is not initialized!");

        if(graphics.IsRendering)
            throw new InvalidOperationException("Cant create an Entity during rendering phase.");

        if(isPreparing)
            throw new InvalidOperationException("Cant create an Entity during preparing phase.");

        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        entityID = new EntityID(entityID.Value + 1);
        container.Allocate(entityID);
        entities[entityID] = entityType; 

        return entityID;
    }
    public bool EntityExists(EntityID entityID)
    {
        return entities.ContainsKey(entityID);
    }
    public bool DestroyEntity(EntityID entityID)
    {
        if(graphics.IsRendering)
            throw new InvalidOperationException("Cant destroy an Entity during rendering phase.");

        if(isPreparing)
            throw new InvalidOperationException("Cant destroy an Entity during preparing phase.");

        if (!entities.TryGetValue(entityID, out EntityType entityType))
            return false;

        if (!typeContainers.TryGetValue(entityType, out EntityContainer container))
            return false;

        container.Destroy(entityID);
        entities.Remove(entityID);

        if(entityTags.TryGetValue(entityID, out List<string> tags))
        {
            foreach(var tag in tags)
            {
                if(!this.taggedEntities.TryGetValue(tag, out var taggedEntities))
                    continue;
                
                taggedEntities.Remove(entityID);
            }

            entityTags.Remove(entityID);
            listPool.Return(tags);
        }

        return true;
    }

    //Components
    public ref T GetEntityComponent<T>(EntityID entityID) where T : struct
    {
        if(graphics.IsRendering)
            throw new InvalidOperationException("Cant access an Entity component during rendering phase. Use GetEntityReadonlyComponent to get components of entities.");

        if(isPreparing)
            throw new InvalidOperationException("Cant access an Entity component during preparing phase. Use GetEntityReadonlyComponent to get components of entities.");

        if (!entities.TryGetValue(entityID, out EntityType entityType))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        return ref typeContainers[entityType].GetComponent<T>(entityID);
    }
    public ref readonly T GetEntityReadonlyComponent<T>(EntityID entityID) where T : struct
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        return ref typeContainers[entityType].GetComponent<T>(entityID);
    }
    public bool HasComponent<T>(EntityType entityType) where T : struct
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        return container.HasComponent<T>();
    }
    
    //Tags
    public void AddEntityTag(EntityID entityID, string tag)
    {
        if(isPreparing)
            throw new InvalidOperationException("Cant add a tag to an Entity during preparing phase.");

        if (!entities.ContainsKey(entityID))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        if(!this.taggedEntities.TryGetValue(tag, out var taggedEntities))
        {
            taggedEntities = new();
            this.taggedEntities[tag] = taggedEntities;
        }

        if(!taggedEntities.TryAdd(entityID))
            return;

        if(!entityTags.TryGetValue(entityID, out var tags))
        {
            tags = listPool.Rent();
            entityTags[entityID] = tags;
        }

        tags.Add(tag);
    }
    public bool EntityHasTag(EntityID entityID, string tag)
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        if (entityType.HasTag(tag))
            return true;
        
        if(!this.taggedEntities.TryGetValue(tag, out var taggedEntities))
            return false;
        
        return taggedEntities.Contains(entityID);
    }
    public ReadOnlySpan<string> GetEntityTags(EntityID entityID)
    {
        if (!entities.ContainsKey(entityID))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        if(!this.entityTags.TryGetValue(entityID, out var tags))
            return ReadOnlySpan<string>.Empty;
        
        return CollectionsMarshal.AsSpan(tags);
    }
    public ReadOnlySpan<EntityID> GetTaggedEntities(string tag)
    {
        if(!this.taggedEntities.TryGetValue(tag, out var taggedEntities))
            return ReadOnlySpan<EntityID>.Empty;
        
        return taggedEntities.AsSpan();
    }

    //EntityType
    public EntityType GetEntityType(EntityID entityID)
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        return entityType;
    }
    public EntityEnumerable GetEntities(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        return new(container);
    }
    public EntitySystem GetEntitySystem(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        return container.EntitySystem;
    }
    public int GetEntityCount(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        return container.EntityCount;
    }
    
    //GameObjects
    public void AddGameObject(GameObject gameObject)
    {
        gameObjects.Add(gameObject.GetType(), gameObject);
    }
    public bool TryAddGameObject(GameObject gameObject)
    {
        return gameObjects.TryAdd(gameObject.GetType(), gameObject);
    }
    public void RemoveGameObject(GameObject gameObject) {
        gameObjects.Remove(gameObject.GetType());
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
    
    //both
    public void Clear()
    {
        gameObjects.Clear();
        entities.Clear();
        entityID = new();
        for(int i = 0; i < entityContainers.Length; i++)
        {
            var container = entityContainers[i];
            container.Clear();
        }

        foreach(var taggedEntities in this.taggedEntities.Values)
        {
            taggedEntities.Clear();
        }
    }
    
    internal EntityContainer[] CreateEntityContainers(ReadOnlySpan<EntityType> entityTypes)
    {
        MethodInfo[] GetMethodInfos(EntitySystem entitySystem, string methodName)
        {
            var result = new List<MethodInfo>();
            var seenVirtualSlots = new HashSet<MethodInfo>();

            Type current = entitySystem.GetType();
            while(current != null && current != typeof(EntitySystem))
            {
                var methodInfos = current.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == methodName);

                foreach(var method in methodInfos)
                {
                    if(method.IsVirtual)
                    {
                        var baseDef = method.GetBaseDefinition();
                        if (seenVirtualSlots.Contains(baseDef))
                            continue;

                        seenVirtualSlots.Add(baseDef);
                    }

                    result.Add(method);
                }

                current = current.BaseType;
            }

            return result.ToArray();
        }
        EntityContainer.Method[] GetMethods(EntityType entityType, EntitySystem entitySystem, string methodName, Dictionary<Type, IComponentContainer> componentContainerMap, bool isByInOnly)
        {
            try
            {
                if(entitySystem == null)
                    return [];

                MethodInfo[] methodInfos = GetMethodInfos(entitySystem, methodName);
                List<EntityContainer.Method> updateMethods = null;

                for(int m = 0; m < methodInfos.Length; m++)
                {
                    try
                    {
                        EntityContainer.Method method = new EntityContainer.Method(entityType, methodInfos[m], entitySystem, componentContainerMap, isByInOnly);
                        updateMethods ??= new List<EntityContainer.Method>(methodInfos.Length);
                        updateMethods.Add(method);
                    }
                    catch(Exception ex)
                    {
                        var paramList = string.Join(", ", methodInfos[m].GetParameters()
                            .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

                        Logger.NewLog(Logger.Log.Level.Error, $"Failed to bind method '{methodInfos[m].DeclaringType?.Name}.{methodInfos[m].Name}({paramList})': {ex}");
                    }
                }

                return updateMethods?.ToArray() ?? null;
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to get {methodName} methods of {entitySystem.GetType().Name}, reason: {ex}");
                return null;
            }
        }

        List<EntityContainer> entityContainers = new List<EntityContainer>(entityTypes.Length);

        uint entryIndex = 0;
        foreach(EntityType entityType in entityTypes)
        {
            EntitySystem entitySystem = entityType.GetEntitySystem();

            IComponentContainer[] componentContainerArray = entityType.GetComponentContainers();

            Dictionary<Type, IComponentContainer> componentContainerMap = new Dictionary<Type, IComponentContainer>();
            for(int i = 0; i < componentContainerArray.Length; i++)
            {
                componentContainerMap ??= [];
                componentContainerMap.Add(componentContainerArray[i].ComponentType.Type, componentContainerArray[i]);
            }

            EntityContainer.Method[] startMethods = GetMethods(entityType, entitySystem, "Start", componentContainerMap, false);
            EntityContainer.Method[] preUpdateMethods = GetMethods(entityType, entitySystem, "Prepare", componentContainerMap, false);
            EntityContainer.Method[] updateMethods = GetMethods(entityType, entitySystem, "Update", componentContainerMap, false);
            EntityContainer.Method[] renderMethods = GetMethods(entityType, entitySystem, "Render", componentContainerMap, true);
            EntityContainer.Method[] destroyMethods = GetMethods(entityType, entitySystem, "OnDestroy", componentContainerMap, false);

            entityContainers.Add(new EntityContainer(entityType, entitySystem, startMethods, preUpdateMethods, updateMethods, renderMethods, destroyMethods, componentContainerArray));
            entryIndex++;
        }

        return entityContainers.ToArray();
    }
    public struct EntityEnumerable : IEnumerable<EntityID>
    {
        private EntityContainer container;
        internal EntityEnumerable(EntityContainer container) => this.container = container;

        public Enumerator GetEnumerator() => new(container);

        IEnumerator<EntityID> IEnumerable<EntityID>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<EntityID>
        {
            internal Enumerator(EntityContainer container)
            {
                this.inner = container.GetEntityEnumerator();
            }
            
            private DenseMap<EntityID, int>.LeftEnumerable.Enumerator inner;
            public EntityID current;
            public EntityID Current => throw new NotImplementedException();

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if(!inner.MoveNext())
                    return false;
                
                current = inner.Current;
                return true;
            }

            public void Reset() => inner.Reset();

            public void Dispose() => inner.Dispose();
        }
    }
}