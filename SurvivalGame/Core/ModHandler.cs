using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using SurvivalGame.Modding;

namespace SurvivalGame.Core;

public static class ModHandler
{
    private struct ModInfo(string name, string modId, string version, string[] tarVersions, string location, string desc = null, string[] dependencies = null, string[] loadAfter = null, string[] loadBefore = null)
    {
        public readonly string Name = name;
        public readonly string ModId = modId;
        public readonly string Description = desc ?? "Mod description";
        public readonly string Version = version;
        public readonly string[] TarVersion = tarVersions;
        public readonly string[] Dependencies = dependencies ?? [];
        public readonly string[] LoadAfter = loadAfter ??  [];
        public readonly string[] LoadBefore = loadBefore ?? [];
        public readonly string Location = location;
    }

    private const string ActiveModsFileName = "active_mods.json";
    private const string ModPicture = "icon";

    private static OrderedDictionary<string, Mod> mods  = null;
    private static OrderedDictionary<string, Mod> activeMods = null;
    private static List<string> activeModsIds_new = null;

    public static bool TryGetModInfo(string modId, out Mod mod)
    {
        return mods.TryGetValue(modId, out mod);
    }

    public static void LoadMods()
    {
        static bool TryGetModInfoLoc(string loc, out ModInfo? modInfo)
        {
            static XElement GetElement(XDocument doc, string elementId, bool important)
            {
                XElement element = doc.Root!.Element(elementId);
                if(important && element == null) throw new InvalidDataException($"'{doc.Root.Name}' is missing <{elementId}> element.");
                return element;
            }
            static List<string> GetElements(XDocument doc, string elementId, string elementsId, bool important)
            {
                List<string> elements = new();
                XElement element = doc.Root!.Element(elementId);
                if(important)
                {
                    if(element == null)
                        throw new InvalidDataException($"'{doc.Root.Name}' is missing <{elementId}> element.");

                    if(!element.Elements(elementsId).Any())
                        throw new InvalidDataException($"'{doc.Root.Name}' element <{elementId}> is missing <{elementId}> elements.");
                }

                foreach (var e in element.Elements(elementsId))
                    elements.Add(e.Value);

                return elements;
            }

            modInfo = null;
            string aboutPath = Path.Combine(loc, "about.xml");
            if (!File.Exists(aboutPath))
                return false;

            var doc = XDocument.Load(aboutPath);
            XElement nameElem = GetElement(doc, "Name", true);
            XElement modIdElem = GetElement(doc, "ModId", true);
            XElement versionElem = GetElement(doc, "Version", true);
            List<string> tarVersions = GetElements(doc, "TarVersion", "ver", true);
            XElement description = GetElement(doc, "Description", false);
            List<string> deps = GetElements(doc, "Dependencies", "modId", true);
            
            modInfo = new ModInfo(
                nameElem.Value,
                modIdElem.Value,
                versionElem.Value,
                tarVersions.ToArray(),
                loc,
                description?.Value,
                deps.ToArray()
            );  

            return true;
        }

        static void LoadFromDisk(out ModInfo[] _modInfos, out HashSet<string> _activeModIds)
        {
            //Load all possible mods
            Dictionary<string, ModInfo> _loadedModInfos = [];
            foreach (var loc in Directory.GetDirectories(SurvivalGame.ModPath))
            {
                if (TryGetModInfoLoc(loc, out ModInfo? info))
                {
                    if(_loadedModInfos.ContainsKey(info.Value.ModId))
                        throw new Exception($"Multiple Mods with the same id: {info.Value.ModId}");
                    _loadedModInfos.Add(info.Value.ModId, info.Value);
                }
            }

            _modInfos = _loadedModInfos.Values.ToArray();

            //Get active mod list
            _activeModIds = [];
            string filePath = Path.Combine(SurvivalGame.AppDataLoc, ActiveModsFileName);
            if (File.Exists(filePath))
            {
                _activeModIds = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(filePath)) ?? _activeModIds;
            }
        }

        static void ActivateMods(HashSet<string> toActivate, ModInfo[] modInfos, out OrderedDictionary<string, Mod> _mods, out Mod[] _activeMods)
        {
            static bool TryLoadAssembly(ModInfo info, out Assembly assembly, out Type modType)
            {   
                string path = Path.Combine(info.Location, $"Assembly.dll");
                assembly = null;
                modType = null;

                if(!File.Exists(path))
                    return false;

                assembly = Assembly.LoadFrom(path);
                modType = assembly.GetTypes().FirstOrDefault(t => typeof(Mod).IsAssignableFrom(t) && !t.IsAbstract) ?? throw new Exception("Assembly doesn't have a Mod type!");
                return true;
            }

            List<Mod> mods = [];
            List<Mod> activeList = [];
            
            foreach (var info in modInfos)
            {
                try
                {
                    Mod modInstance = null;
                    bool isActive = toActivate.Contains(info.ModId);

                    if (isActive && TryLoadAssembly(info, out Assembly assembly, out Type modType))
                    {
                        modInstance = (Mod)Activator.CreateInstance(modType,
                            info.Name, info.ModId, info.Version, info.TarVersion, info.Location,
                            info.Description, info.Dependencies, info.LoadAfter, info.LoadBefore);

                        activeList.Add(modInstance);
                    }
                    else
                        modInstance = new Mod(info.Name, info.ModId, info.Version, info.TarVersion,
                                            info.Location, info.Description, info.Dependencies,
                                            info.LoadAfter, info.LoadBefore);

                    mods.Add(modInstance);
                    
                }
                catch(Exception ex)
                {
                    SurvivalGame.LOGGER.Wrn($"Failed to activate mod '{info.ModId}', exception: {ex.Message}");
                    continue;
                }
            }

            _mods = new OrderedDictionary<string, Mod>(mods.OrderBy(m => m.Name).ToDictionary(m => m.ModId));
            _activeMods = activeList.ToArray();
        }

        mods = [];
        activeMods = [];
        activeModsIds_new = [];

        if(!Directory.Exists(SurvivalGame.ModPath))
        {
            Directory.CreateDirectory(SurvivalGame.ModPath);
            return;
        }
        
        LoadFromDisk(out ModInfo[] _modInfos, out HashSet<string> _activeModIds);
        ActivateMods(_activeModIds, _modInfos, out OrderedDictionary<string, Mod> _mods, out Mod[] _activeMods);

        mods = _mods;
        activeMods = new OrderedDictionary<string, Mod>(
                _activeMods.ToDictionary(m => m.ModId, m => m)
        );
        activeModsIds_new = _activeModIds.ToList();

        //removing unloaded mods from the activeModsIds_new list
        foreach(var modId in _activeModIds)
            if(!mods.ContainsKey(modId))
                activeModsIds_new.Remove(modId);
    }

    public static bool ActivateMod(string modId)
    {
        if(!mods.ContainsKey(modId))
            return false;

        activeModsIds_new.Add(modId);
        return true;
    }
    public static bool DeactiveMod(string modId)
    {
        if(!mods.ContainsKey(modId))
            return false;

        return activeModsIds_new.Remove(modId);
    }

    public static void MoveMod(int index, int toIndex)
    {
        if (index == toIndex) return;

        string item = activeModsIds_new[index];
        activeModsIds_new.RemoveAt(index);

        if (toIndex > index) toIndex--;

        activeModsIds_new.Insert(toIndex, item);
    }

    public static bool SortMods(out string Error)
    {
        Error = null;
        try
        {
            int n = activeModsIds_new.Count;
            var depGraph = new Dictionary<string, HashSet<string>>(n);
            var indegree = new Dictionary<string, int>(n);

            //initialize
            foreach (var modId in activeModsIds_new)
            {
                depGraph[modId] = [];
                indegree[modId] = 0;
            }

            //Adding deps
            foreach (var modId in activeModsIds_new)
            {
                if (!mods.TryGetValue(modId, out var mod)) continue;

                foreach (var dep in mod.Dependencies.Concat(mod.LoadAfter))
                {
                    if (!depGraph.ContainsKey(dep)) continue;
                    depGraph[dep].Add(modId);
                    indegree[modId]++;
                }

                foreach (var dep in mod.LoadBefore)
                {
                    if (!depGraph.ContainsKey(dep)) continue;
                    depGraph[modId].Add(dep);
                    indegree[dep]++;
                }
            }

            var sorted = new List<string>(n);

            //Queue all mods with no dependencies
            var queue = new Queue<string>(n);
            foreach (var kv in indegree)
                if (kv.Value == 0) queue.Enqueue(kv.Key);

            while (queue.Count > 0)
            {
                //first in queue
                var modId = queue.Dequeue();
                sorted.Add(modId);

                //loop over the mods that are dependant on this mod
                foreach (var dependent in depGraph[modId])
                {
                    indegree[dependent]--;
                    if (indegree[dependent] == 0)
                        queue.Enqueue(dependent);
                }
            }

            if (sorted.Count != activeModsIds_new.Count)
                throw new Exception("Circular dependencies detected!");

            activeModsIds_new = sorted;
            return true;
        }
        catch(Exception ex)
        {
            Error = ex.Message;
            return false;
        }
    }
    
    public static void SaveActiveMods()
    {
        string filePath = Path.Combine(SurvivalGame.AppDataLoc, ActiveModsFileName);
        
        try
        {
            string json = JsonSerializer.Serialize(new HashSet<string>(activeModsIds_new), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            SurvivalGame.LOGGER.Err($"Failed to save active mods: {ex.Message}");
        }
    }
    
    public static ICollection<Mod> LoadedMods
    {
        get
        {
            return mods.Values;
        }
    }
    public static ICollection<Mod> ActiveMods
    {
        get
        {
            return activeMods.Values;
        }
    }
}
