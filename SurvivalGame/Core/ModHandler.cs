using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using SurvivalGame.Modding;

namespace SurvivalGame.Core;

public struct ModInfo(string name, string modId, string version, string[] tarVersions, string location, string desc = null, string[] dependencies = null)
{
    public readonly string Name = name;
    public readonly string ModId = modId;
    public readonly string Description = desc ?? "Mod description";
    public readonly string Version = version;
    public readonly string[] TarVersion = tarVersions;
    public readonly string[] Dependencies = dependencies ?? Array.Empty<string>();

    public readonly string Location = location;
    public const string Picture = "icon";

    internal Mod ModInstance = null;
}

public static class ModHandler
{
    private static Dictionary<string, ModInfo> modsIdinfo  = null;
    private static ModInfo[] activeMods = null;
    private static List<string> activeModsIds_new = null;

    public static ICollection<ModInfo> LoadedMods
    {
        get
        {
            return modsIdinfo.Values;
        }
    }
    public static ModInfo[] ActiveMods
    {
        get
        {
            return activeMods;
        }
    }

    public static bool TryGetModInfo(string modId, out ModInfo info)
    {
        return modsIdinfo.TryGetValue(modId, out info);
    }
    private static bool TryGetModInfoLoc(string loc, out ModInfo info)
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

        info = new();
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
        
        info = new ModInfo(
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
    internal static void LoadMods()
    {
        if(!Directory.Exists(SurvivalGame.ModPath))
        {
            Directory.CreateDirectory(SurvivalGame.ModPath);
            return;
        }

        List<string> activeModsIds = [];

        //Load all mods
        foreach (var loc in Directory.GetDirectories(SurvivalGame.ModPath))
        {
            if (TryGetModInfoLoc(loc, out ModInfo info))
            {
                modsIdinfo[info.ModId] = info;
            }
        }

        //Get active mod list
        string filePath = Path.Combine(SurvivalGame.AppDataLoc, "active_mods.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            activeModsIds = JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }

        //activating mods
        foreach(var modId in activeModsIds)
        {
            if(TryGetModInfo(modId, out ModInfo info))
            {
                Mod mod = new(info.ModId);

                string assemblyPath = Path.Combine(info.Location, "Assembly.dll");
                if (File.Exists(assemblyPath))
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);
                        Type modType = assembly.GetTypes().FirstOrDefault(t => typeof(Mod).IsAssignableFrom(t) && !t.IsAbstract)
                            ?? throw new Exception($"{info.Name} assembly doesn't have a Mod class!");

                        mod = (Mod)Activator.CreateInstance(modType, info.ModId)!;
                        mod.Assembly = assembly;
                    }
                    catch (Exception ex)
                    {
                        SurvivalGame.LOGGER.Log($"Failed to load mod assembly at {assemblyPath}: {ex.Message}");
                    }
                }

                mod.OnLoad();

                info.ModInstance = mod; 
            }
            else  { // Mod didnt get loaded
                SurvivalGame.LOGGER.Wrn($"Mod '{modId}' not found!");
                activeModsIds.Remove(modId);
            }
        }

        activeModsIds_new = activeModsIds;
    }
}
