// using Luminix.RegistrySystem;

// namespace Luminix;


// internal void FinalizeRegistry()
// {
//     IStructContainer[] GetStructContainers(Type[] structTypes)
//     {
//         List<IStructContainer> structContainers = new List<IStructContainer>(structTypes.Length);

//         IStructContainer idContainer = (IStructContainer)Activator.CreateInstance(typeof(StructContainer<ID>));
//         structContainers.Add(idContainer);

//         IStructContainer boolContainer = (IStructContainer)Activator.CreateInstance(typeof(StructContainer<bool>));
//         structContainers.Add(boolContainer);

//         foreach(Type structType in structTypes)
//         {
//             if(!structType.IsValueType)
//             {
//                 Logger.Get().Err($"Unable to create component container for type '{structType.FullName}', reason: Provided type is not a ValueType!");
//                 continue;
//             }

//             if(structType == typeof(ID))
//             {
//                 Logger.Get().Err($"Unable to create component container for type '{structType.FullName}', reason: Provided type is built in!");
//                 continue;
//             }

//             if(structType == typeof(bool))
//             {
//                 Logger.Get().Err($"Unable to create component container for type '{structType.FullName}', reason: Provided type is built in!");
//                 continue;
//             }

//             Type closedType = typeof(StructContainer<>).MakeGenericType(structType);
//             IStructContainer structContainer = (IStructContainer)Activator.CreateInstance(closedType);

//             structContainers.Add(structContainer);
//         }

//         return structContainers.ToArray();
//     }
//     List<MethodInfo> GetMethodInfos(EntitySystem entitySystem, string methodName)
//     {
//         var result = new List<MethodInfo>();
//         var seenVirtualSlots = new HashSet<MethodInfo>();

//         Type current = entitySystem.GetType();
//         while(current != null && current != typeof(EntitySystem))
//         {
//             var methodInfos = current.GetMethods(
//                 BindingFlags.Instance |
//                 BindingFlags.Public |
//                 BindingFlags.NonPublic |
//                 BindingFlags.DeclaredOnly)
//                 .Where(m => m.Name == methodName);

//             foreach(var method in methodInfos)
//             {
//                 if(method.IsVirtual)
//                 {
//                     var baseDef = method.GetBaseDefinition();
//                     if (seenVirtualSlots.Contains(baseDef))
//                         continue;

//                     seenVirtualSlots.Add(baseDef);
//                 }

//                 result.Add(method);
//             }

//             current = current.BaseType;
//         }

//         return result;
//     }
//     List<EntityContainer.Method> GetMethods(EntitySystem entitySystem, string methodName, Dictionary<Type, IStructContainer> structContainerMap)
//     {
//         List<MethodInfo> methodInfos = GetMethodInfos(entitySystem, methodName);
//         List<EntityContainer.Method> updateMethods = null;

//         for(int m = 0; m < methodInfos.Count; m++)
//         {
//             try
//             {
//                 EntityContainer.Method method = new EntityContainer.Method(methodInfos[m], entitySystem, structContainerMap);
//                 updateMethods ??= new List<EntityContainer.Method>(methodInfos.Count);
//                 updateMethods.Add(method);
//             }
//             catch(Exception ex)
//             {
//                 var paramList = string.Join(", ", methodInfos[m].GetParameters()
//                     .Select(p => $"{(p.ParameterType.IsByRef ? "ref " : "")}{(p.ParameterType.GetElementType() ?? p.ParameterType).Name} {p.Name}"));

//                 Logger.Get().Err($"Failed to bind method '{methodInfos[m].DeclaringType?.Name}.{methodInfos[m].Name}({paramList})': {ex.Message}");
//             }
//         }

//         return updateMethods;
//     }

//     List<EntityContainer> entityContainers = new List<EntityContainer>(entries.Count);

//     uint entryIndex = 0;
//     foreach(Entry entry in entries.Values)
//     {
//         var entityType = new EntityType(entry.TypeName, (ID)entryIndex);

//         EntitySystem entitySystem = (EntitySystem)Activator.CreateInstance(entry.EntitySystemType, entityType);
//         Type[] structTypes = entitySystem.GetComponentTypes();

//         IStructContainer[] structContainerArray = GetStructContainers(structTypes);

//         Dictionary<Type, IStructContainer> structContainerMap = new Dictionary<Type, IStructContainer>();
//         for(int i = 0; i < structContainerArray.Length; i++)
//         {
//             structContainerMap ??= [];
//             structContainerMap.Add(structContainerArray[i].StructType, structContainerArray[i]);
//         }

//         List<EntityContainer.Method> startMethods = null;
//         List<EntityContainer.Method> updateMethods = null;
//         List<EntityContainer.Method> renderMethods = null;
//         List<EntityContainer.Method> destroyMethods = null;

//         try
//         {
//             startMethods = GetMethods(entitySystem, "Start", structContainerMap);
//         }
//         catch(Exception ex)
//         {
//             Logger.Get().Err($"Failed to get Start methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
//         }

//         try
//         {
//             updateMethods = GetMethods(entitySystem, "Update", structContainerMap);
//         }
//         catch(Exception ex)
//         {
//             Logger.Get().Err($"Failed to get Update methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
//         }

//         try
//         {
//             renderMethods = GetMethods(entitySystem, "Render", structContainerMap);
//         }
//         catch(Exception ex)
//         {
//             Logger.Get().Err($"Failed to get Render methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
//         }

//         try
//         {
//             destroyMethods = GetMethods(entitySystem, "OnDestroy", structContainerMap);
//         }
//         catch(Exception ex)
//         {
//             Logger.Get().Err($"Failed to get OnDestroy methods of {entitySystem.GetType().Name}, reason: {ex.Message}");
//         }


//         entityContainers.Add(new EntityContainer(entityType, entitySystem, startMethods.ToArray(), updateMethods.ToArray(), renderMethods.ToArray(), destroyMethods.ToArray(), structContainerArray));
//         entryIndex++;
//     }

//     finalizedEntityContainers = entityContainers.ToArray();
// }