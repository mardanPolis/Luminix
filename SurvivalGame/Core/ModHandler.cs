using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public readonly string[] Dependencies = dependencies;

    public readonly string Location = location;
    public const string Picture = "icon";
}

public static class ModHandler
{
    public static Mod[] Mods { private set; get; } = null;
    private static Dictionary<string, Mod> activeMods = null;
    public static Mod[] ActiveMods
    {
        get
        {
            return activeMods.Values.ToArray();
        }
    }

    internal static void LoadMods()
    {
        foreach (var loc in Directory.GetDirectories(SurvivalGame.ModPath))
        {
            if(TryGetModInfo(loc, out ModInfo info))
            {
                
            }
        }
    }
    private static bool TryGetModInfo(string loc, out ModInfo info)
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

            foreach (var e in element.Elements("li"))
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

    private static Mod ActivateMod(ModInfo info)
    {
        Mod mod;
        Assembly assembly = null;
        string assemblyPath = Path.Combine(info.Location, "Assembly.dll");
        
        if (File.Exists(assemblyPath))
        {
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);

                Type modType = assembly.GetTypes().FirstOrDefault(t => typeof(Mod).IsAssignableFrom(t) && !t.IsAbstract)
                    ?? throw new Exception($"{info.Name} assembly doesn't have a Mod class!");

                mod = (Mod)Activator.CreateInstance(modType)!;
                return mod;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load mod assembly at {assemblyPath}: {ex.Message}");
            }
        }

        mod = new();
        mod.Info = info;
        mod.Assembly = assembly;

        mod.OnLoad();

        return mod; 
    }
}
