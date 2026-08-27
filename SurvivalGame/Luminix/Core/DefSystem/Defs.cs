using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Luminix.Collections;
using Luminix.Exceptions;
using Luminix.Extensions;
using Luminix.JsonContext;

namespace Luminix;

public sealed class Defs : Workspace.Service
{
    internal Defs()
    {
        serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        defTypes = new Dictionary<string, Type>();
        patchTypes = new Dictionary<string, Type>();

        defNodes = [];
        defs = new Dictionary<(Type, string), Def>();
        defContainers = new Dictionary<Type, IList>();
        defList = [];
    }

    public sealed class Token
    {
        internal Token() { }
    }

    private JsonSerializerOptions serializerOptions;

    private IDictionary<string, Type> defTypes;
    private IDictionary<string, Type> patchTypes;

    private Dictionary<(Type, string), DefNode> defNodes;
    private List<PatchNode> patchNodes;

    private List<Def> defList;
    private IDictionary<Type, IList> defContainers;
    private IDictionary<(Type, string), Def> defs;

    private Token cachedToken;


    public void NewPatchType<T>() where T : PatchNode
    {
        Type type = typeof(T);

        if(patchTypes.ContainsKey(type.Name))
            return;

        patchTypes[type.Name] = type;
    }
    public void NewDefType<T>() where T : Def
    {
        Type type = typeof(T);

        if(defTypes.ContainsKey(type.Name))
            return;

        defTypes[type.Name] = type;
        defContainers[type] = new List<T>();
    }

    public void ParseDataAsset(DataAsset dataAsset)
    {
        JsonDocument json = dataAsset.Json;
        JsonElement root = json.RootElement;

        try
        {
            if (root.TryGetProperty("defType", out JsonElement type))
            {
                var defType = defTypes[type.GetProperty("defType").GetString()];
                var defNode = type.Deserialize(LuminixJsonContext.Default.DefNode);
                defNodes[(defType, defNode.DefName)] = defNode;
            }
            else if (root.TryGetProperty("patchType", out type))
            {
                var patchType = patchTypes[type.GetString()];
                var patchNode = (PatchNode)root.Deserialize(patchType, serializerOptions);
                patchNodes.Add(patchNode);
            }
            else
            {
                logger.NewLog(Logger.Log.Level.Error, "Unable to parse dataAsset to a node, reason: DataAsset has neither 'defType' nor 'patchType' — cannot determine data kind.");
            }
        }
        catch (Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"Unable to parse dataAsset to a node, reason: {ex}");
        }
    }

    internal void PatchNodes()
    {
        foreach(var patchNode in patchNodes)
        {
            var id = patchNode.DefID.Split('/');;
            string defType = id[1];
            string defName = id[1];

            if(id.Length != 2)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve patch DefID-'{patchNode.DefID}', reason: expected format 'DefType/DefName'");
                continue;
            }
            if(!defTypes.TryGetValue(defType, out Type type))
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve patch DefID-'{patchNode.DefID}', reason: unknown Def Type of '{defType}'");
                continue;
            }
            if(!defNodes.TryGetValue((type, defName), out var target))
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to patch def '{patchNode.DefID}', reason: Failed to find def with defID '{patchNode.DefID}'");
                continue;
            }

            try
            {
                patchNode.Patch(target);
            }
            catch(Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to patch def '{patchNode.DefID}', reason: {ex}");
            }
        }

        patchNodes = null;
    }
    internal void InheritNodes()
    {
        List<DefNode> parentLessNodes;
        Dictionary<DefNode, List<DefNode>> defNodeChildren;

        bool CreateInheritanceTree()
        {
            try
            {
                //creating
                foreach (var defNode in defNodes.Values)
                {
                    var hasParent = !string.IsNullOrWhiteSpace(defNode.Parent) && !defNode.Parent.HasWhitespace();
                    if (!hasParent)
                    {
                        parentLessNodes.Add(defNode);
                        continue;
                    }

                    var parentID = (defTypes[defNode.DefType], defNode.Parent);

                    if(!defNodes.TryGetValue(parentID, out var parentNode))
                    {
                        logger.NewLog(Logger.Log.Level.Error, $"Unable to insert a def into inheritance tree, reason: def '{defNode.DefName}' references unknown parent '{defNode.Parent}'");
                        continue;
                    }

                    if (!defNodeChildren.TryGetValue(parentNode, out var children))
                    {
                        children = new List<DefNode>();
                        defNodeChildren[parentNode] = children;
                    }

                    children.Add(defNode);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to create inheritance tree, reason: {ex}");
                return false;
            }
        }
        bool CheckForInheritanceCycles()
        {

            try
            {
                var visited = new HashSet<DefNode>();
                var stack = new Stack<DefNode>(parentLessNodes);

                while(stack.Count > 0)
                {
                    var node = stack.Pop();
                    visited.Add(node);

                    if (defNodeChildren.TryGetValue(node, out var children))
                        foreach (var child in children)
                            stack.Push(child);
                }
                
                if(defNodes.Count == visited.Count) // no node are in a cycle
                    return true;

                var unreachableNodes = defNodeChildren.Keys
                    .Concat(defNodeChildren.Values.SelectMany(c => c))
                    .Where(node => !visited.Contains(node))
                    .Distinct()
                    .ToList();

                var reportedCycles = new HashSet<DefNode>();
                foreach (var node in unreachableNodes)
                {
                    if (reportedCycles.Contains(node))
                        continue;

                    var path = new List<DefNode>();
                    var seen = new HashSet<DefNode>();
                    var current = node;

                    while (seen.Add(current))
                    {
                        path.Add(current);
                        var parentID = (defTypes[current.DefType], current.Parent);
                        current = defNodes[parentID];
                    }

                    var cycleStartIndex = path.IndexOf(current);
                    var cycle = path.Skip(cycleStartIndex).ToList();
                    foreach (var cycleNode in cycle)
                        reportedCycles.Add(cycleNode);

                    var cycleDescription = string.Join(" -> ", cycle.Select(n => n.DefName));
                    logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve inheritance, reason: cycle detected: {cycleDescription} -> {cycle[0].DefName}");
                }
                return true;
            }
            catch(Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve def node inheritance, reason: {ex}");
                return false;
            }
        }
        void HandleInheritance(DefNode parent)
        {
            Dictionary<string, JsonElement> childProperties;
            Dictionary<string, JsonElement> parentProperties;

            void InheritProperties()
            {
                foreach (var (key, parentValue) in parentProperties)
                {
                    if (childProperties.TryGetValue(key, out var childValue))
                    {
                        childProperties[key] = childValue;

                        if (parentValue.ValueKind != JsonValueKind.Object && childValue.ValueKind != JsonValueKind.Object)
                            continue;

                        var parentObj = JsonObject.Create(parentValue)!;
                        var childObj = JsonObject.Create(childValue)!;

                        MergeJsonObjects(parentObj, childObj);

                        childProperties[key] = JsonSerializer.SerializeToElement(childObj);
                    }
                    else
                    {
                        childProperties[key] = parentValue;
                    }
                }
            }
            void MergeJsonObjects(JsonObject parent, JsonObject child)
            {
                foreach (var (key, parentNode) in parent)
                {
                    if (child.TryGetPropertyValue(key, out var childNode) && childNode is not null)
                    {
                        if (parentNode is JsonObject parentChildObj && childNode is JsonObject childChildObj)
                            MergeJsonObjects(parentChildObj, childChildObj);
                    }
                    else
                    {
                        child[key] = parentNode?.DeepClone();
                    }
                }
            }

            if(!defNodeChildren.TryGetValue(parent, out var children))
                return;

            foreach(var child in children)
            {
                try
                {
                    childProperties = child.Properties;
                    parentProperties = parent.Properties;

                    InheritProperties();
                }
                catch (Exception ex)
                {
                    logger.NewLog(Logger.Log.Level.Error, $"Failed to inherit properties for def '{child.DefName}' from parent '{parent.DefName}', reason: {ex}");
                    continue;
                }

                HandleInheritance(child);
            }
        }
        
        try
        {
            parentLessNodes = [];
            defNodeChildren = [];

            if(!CreateInheritanceTree()) return;
            if(!CheckForInheritanceCycles()) return;

            Parallel.ForEach(parentLessNodes, defNode => HandleInheritance(defNode));
        }
        catch(Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"Failed to handle def inheritance, reason: {ex}");
        }
    }
    internal void DeserializeNodes()
    {
        var constructors = GetDefConstructors();

        foreach(var defNode in defNodes.Values)
        {
            if(defNode.Abstract)
                continue;

            if(string.IsNullOrWhiteSpace(defNode.DefType) || defNode.DefType.Any(char.IsWhiteSpace))
            {
                logger.NewLog(Logger.Log.Level.Error, "Unable to Parse DefNode, reason: Provided 'DefType' is null, empty or has whitespaces.");
                continue;
            }

            if(string.IsNullOrWhiteSpace(defNode.DefName) || defNode.DefName.Any(char.IsWhiteSpace))
            {
                logger.NewLog(Logger.Log.Level.Error, "Unable to Parse DefNode, reason: Provided 'DefName' is null, empty or has whitespaces.");
                continue;
            }

            Type defType = defTypes[defNode.DefType];

            try
            {
                Def def = ParseDefNode(defNode, constructors[defType]);
                defs[(defType, def.Name)] = def;
                defContainers[defType].Add(def);
                defList.Add(def);
            }
            catch(Exception ex)
            {
                logger.NewLog(Logger.Log.Level.Error, $"Unable to parse DefNode to {defType.FullName}, reason: {ex}");
            }
        }
    }

    internal void FinalizeTypes()
    {
        defTypes = defTypes.ToFrozenDictionary();
        patchTypes = patchTypes.ToFrozenDictionary();
    }
    internal void FinalizeDefs()
    {
        defContainers = defContainers.ToFrozenDictionary();
        defs = defs.ToFrozenDictionary();

        MethodInfo initializeMethodInfo = typeof(Def).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
        Action<Def> initializeMethod = (Action<Def>)Delegate.CreateDelegate(typeof(Action<Def>), initializeMethodInfo);

        foreach(var def in defs.Values)
        {
            initializeMethod(def);
        }
    }

    internal void ValidateToken(Token token)
    {
        if (!cachedToken.Equals(token))
            throw new InvalidTokenException();

        cachedToken = null;
    }

    private Def ParseDefNode(DefNode defNode, (ConstructorInfo, Dictionary<string, (int, ParameterInfo)>) constructorData)
    {
        var (constructor, parameters) = constructorData;
        var properties = defNode.Properties;

        var arguments = new object[parameters.Count];
        foreach(var param in parameters.Values)
            arguments[param.Item1] = param.Item2.DefaultValue;

        Token token = new Token();
        cachedToken = token;

        arguments[0] = token;
        arguments[1] = defNode.DefName;

        void PopulateArguments()
        {
            List<string> unresolvedProperties = null;
            foreach(var prop in properties)
            {
                if(!parameters.TryGetValue(prop.Key, out (int ParameterIndex, ParameterInfo ParameterInfo) parameter))
                {
                    unresolvedProperties ??= [];
                    unresolvedProperties.Add(prop.Key);
                    continue;
                }

                arguments[parameter.ParameterIndex] = prop.Value.Deserialize(parameter.ParameterInfo.ParameterType, serializerOptions);
            }

            if (unresolvedProperties != null)
                throw new InvalidOperationException($"Unresolved properties - {string.Join(", ", unresolvedProperties)}");
        }
        PopulateArguments();

        return (Def)constructor.Invoke(arguments);
    }
    private Dictionary<Type, (ConstructorInfo, Dictionary<string, (int, ParameterInfo)>)> GetDefConstructors()
    {
        var constructors = new Dictionary<Type, (ConstructorInfo, Dictionary<string, (int, ParameterInfo)>)>();

        foreach(var type in defTypes.Values)
        {
            if(defTypes.ContainsKey(type.Name))
                continue;

            var constructor = type.GetConstructors().Single();

            var parameters = constructor.GetParameters();
            if (parameters.Length < 2
                || parameters[0].ParameterType != typeof(Defs.Token)
                || parameters[1].ParameterType != typeof(string))
            {
                throw new InvalidOperationException($"Constructor for '{type.Name}' must start with (Defs.Token, string, ...), but found ({string.Join(", ", parameters.Select(p => p.ParameterType.Name))}).");
            }

            var parametersMap = new Dictionary<string, (int, ParameterInfo)>(parameters.Length);
            for(int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                parametersMap.Add(parameter.Name, (i, parameter));
            }

            constructors[type] = (constructor, parametersMap);
        }

        return constructors;
    }
}