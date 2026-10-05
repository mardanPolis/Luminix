using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Luminix.Collections;

namespace Luminix;

public sealed class World : Workspace.Service
{
    internal readonly struct Controller 
    {
        internal Controller(World controlled)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a World.Controller, the provided universe is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly World controlled;

        public readonly void Tick()
        {
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
    }

    private Graphics graphics;

    private readonly bool controllerIssued;

    private EntityID entityID;

    private Dictionary<EntityID, EntityType> entities;

    private Dictionary<EntityType, EntityContainer> typeContainers;
    private EntityContainer[] entityContainers;

    private bool isPreparing;

    protected override void Start()
    {
        var defs = Workspace.Current.GetService<Defs>();
        var entityTypes = defs.GetDefs<EntityType>();
        entityContainers = CreateEntityContainers(entityTypes);
        entities = [];
        typeContainers = [];
        
        foreach(EntityContainer container in entityContainers)
            typeContainers[container.EntityType] = container;

        foreach(EntityContainer container in entityContainers)
            container.EntitySystem?.Initialize(container.EntityType);
    }

    public EntityID CreateEntity(EntityType entityType)
    {
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
        return true;
    }

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

    public EntityType GetEntityType(EntityID entityID)
    {
        if (!entities.TryGetValue(entityID, out EntityType entityType))
            throw new ArgumentException($"Entity does not exist", nameof(entityID));

        return entityType;
    }
    
    public bool HasComponent<T>(EntityType entityType) where T : struct
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
            throw new ArgumentException("Unknown entity type", nameof(entityType));

        return container.HasComponent<T>();
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
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        return container.EntitySystem;
    }
    public int GetEntityCount(EntityType entityType)
    {
        if(!typeContainers.TryGetValue(entityType, out EntityContainer container))
        {
            throw new ArgumentException("Unknown entity type", nameof(entityType));
        }

        return container.EntityCount;
    }
    
    public void Clear()
    {
        //TODO
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
        EntityContainer.Method[] GetMethods(EntitySystem entitySystem, string methodName, Dictionary<Type, IComponentContainer> componentContainerMap, bool isByInOnly)
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
                        EntityContainer.Method method = new EntityContainer.Method(methodInfos[m], entitySystem, componentContainerMap, isByInOnly);
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

            EntityContainer.Method[] startMethods = GetMethods(entitySystem, "Start", componentContainerMap, false);
            EntityContainer.Method[] preUpdateMethods = GetMethods(entitySystem, "Prepare", componentContainerMap, false);
            EntityContainer.Method[] updateMethods = GetMethods(entitySystem, "Update", componentContainerMap, false);
            EntityContainer.Method[] renderMethods = GetMethods(entitySystem, "Render", componentContainerMap, true);
            EntityContainer.Method[] destroyMethods = GetMethods(entitySystem, "OnDestroy", componentContainerMap, false);

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