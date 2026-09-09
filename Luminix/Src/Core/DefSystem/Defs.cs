using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Luminix.Extensions;
using Luminix.Json;

namespace Luminix;

public sealed class Defs : Workspace.Service
{
    internal Defs()
    {
        defTypes = new Dictionary<string, Type>();
        patchTypes = new Dictionary<string, Type>();

        defNodes = [];
        patchNodes = [];
        defs = new Dictionary<(Type, string), Def>();
        defContainers = new Dictionary<Type, IList>();
        defList = [];
    }

    private IDictionary<string, Type> defTypes;
    private IDictionary<string, Type> patchTypes;

    private Dictionary<(Type, string), DefNode> defNodes;
    private List<PatchNode> patchNodes;

    private List<Def> defList;
    private IDictionary<Type, IList> defContainers;
    private IDictionary<(Type, string), Def> defs;

    public bool IsFinalized { get; private set; }

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

    public T GetDef<T>(string name) where T : Def
    {
        if (!defs.TryGetValue((typeof(T), name), out Def def))
            throw new KeyNotFoundException($"No {typeof(T).Name} def named \"{name}\" was found.");

        return (T)def;
    }

    public ReadOnlySpan<T> GetDefs<T>() where T : Def
    {
        if (!defContainers.TryGetValue(typeof(T), out IList container))
            throw new InvalidOperationException($"No defs of type {typeof(T).Name} have been registered.");

        return CollectionsMarshal.AsSpan((List<T>)container);
    }

    public ReadOnlySpan<Def> GetDefs()
    {
        return CollectionsMarshal.AsSpan(defList);
    }

    public void ParseDataAsset(DataAsset dataAsset)
    {
        JsonDocument json = dataAsset.Json;
        JsonElement root = json.RootElement;

        try
        {
            if (root.TryGetProperty("DefType", out JsonElement type) || root.TryGetProperty("defType", out type))
            {
                var defType = defTypes[type.GetString()];
                var defNode = LuminixJsonUtility.Deserialize<DefNode>(root);
                defNodes[(defType, defNode.DefName)] = defNode;
            }
            else if (root.TryGetProperty("PatchType", out type) || root.TryGetProperty("patchType", out type))
            {
                var patchType = patchTypes[type.GetString()];
                var patchNode = LuminixJsonUtility.Deserialize<PatchNode>(root);
                patchNodes.Add(patchNode);
            }
            else
            {
                Logger.NewLog(Logger.Log.Level.Error, "Unable to parse dataAsset to a node, reason: DataAsset has neither 'defType' nor 'patchType' — cannot determine data kind.");
            }
        }
        catch (Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Unable to parse dataAsset to a node, reason: {ex}");
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
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve patch DefID-'{patchNode.DefID}', reason: expected format 'DefType/DefName'");
                continue;
            }
            if(!defTypes.TryGetValue(defType, out Type type))
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve patch DefID-'{patchNode.DefID}', reason: unknown Def Type of '{defType}'");
                continue;
            }
            if(!defNodes.TryGetValue((type, defName), out var target))
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to patch def '{patchNode.DefID}', reason: Failed to find def with defID '{patchNode.DefID}'");
                continue;
            }

            try
            {
                patchNode.Patch(target);
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to patch def '{patchNode.DefID}', reason: {ex}");
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
                        Logger.NewLog(Logger.Log.Level.Error, $"Unable to insert a def into inheritance tree, reason: def '{defNode.DefName}' references unknown parent '{defNode.Parent}'");
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
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to create inheritance tree, reason: {ex}");
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
                    Logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve inheritance, reason: cycle detected: {cycleDescription} -> {cycle[0].DefName}");
                }
                return true;
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to resolve def node inheritance, reason: {ex}");
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

                        childProperties[key] = LuminixJsonUtility.MergeJsonElements(parentValue, childValue);
                    }
                    else
                    {
                        childProperties[key] = parentValue;
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
                    Logger.NewLog(Logger.Log.Level.Error, $"Failed to inherit properties for def '{child.DefName}' from parent '{parent.DefName}', reason: {ex}");
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
            Logger.NewLog(Logger.Log.Level.Error, $"Failed to handle def inheritance, reason: {ex}");
        }
    }
    internal void DeserializeNodes()
    {
        foreach(var defNode in defNodes.Values)
        {
            if(defNode.Abstract)
                continue;

            if(string.IsNullOrWhiteSpace(defNode.DefType) || defNode.DefType.Any(char.IsWhiteSpace))
            {
                Logger.NewLog(Logger.Log.Level.Error, "Unable to Parse DefNode, reason: Provided 'DefType' is null, empty or has whitespaces.");
                continue;
            }

            if(string.IsNullOrWhiteSpace(defNode.DefName) || defNode.DefName.Any(char.IsWhiteSpace))
            {
                Logger.NewLog(Logger.Log.Level.Error, "Unable to Parse DefNode, reason: Provided 'DefName' is null, empty or has whitespaces.");
                continue;
            }

            Type defType = defTypes[defNode.DefType];

            try
            {
                Def def = ParseDefNode(defNode);
                defs[(defType, def.Name)] = def;
                defContainers[defType].Add(def);
                defList.Add(def);
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Unable to parse DefNode to {defType.FullName}, reason: {ex}");
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

        IsFinalized = true;

        MethodInfo initializeMethodInfo = typeof(Def).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
        Action<Def> initializeMethod = (Action<Def>)Delegate.CreateDelegate(typeof(Action<Def>), initializeMethodInfo);

        foreach(var def in defs.Values)
        {
            initializeMethod(def);
        }
    }

    private Def ParseDefNode(DefNode defNode)
    {
        Type defType = defTypes[defNode.DefType];
        
        JsonObject defJson = LuminixJsonUtility.Parse(defNode.Properties);
        defJson["name"] = defNode.DefName;

        return LuminixJsonUtility.Deserialize(defJson, defType) as Def;
    }
}