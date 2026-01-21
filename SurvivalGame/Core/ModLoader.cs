using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using SurvivalGame.Modding;

namespace SurvivalGame.Core;

public struct ModInfo
{
    public readonly string Name;
    public readonly string ModId;
    public readonly string Version;
    public const string Picture = "icon";
    public readonly string Description;
    public readonly string[] Dependencies;

    public ModInfo(string name, string modId, string version, string desc = null, string[] dependencies = null)
    {
        Name = name;
        ModId = modId;
        Version = version;
        Description = desc ?? "Mod description";
        Dependencies = dependencies;
    }
}

public static class ModLoader
{
    public static List<ModInfo> LoadMods()
    {
        var mods = new List<ModInfo>();

        foreach (var dir in Directory.GetDirectories(SurvivalGame.modPath))
        {
            string aboutPath = Path.Combine(dir, "about.xml");
            if (!File.Exists(aboutPath))
                continue;

            mods.Add(LoadMod(dir));
        }

        return mods;
    }

    private static ModInfo LoadMod(string path)
    {
        
        var doc = XDocument.Load(Path.Combine(path, "about.xml"));
        var root = doc.Root!;

        var deps = root.Element("Dependencies");
        List<string> depsArray = new();
        if (deps != null)
        {
            foreach (var e in deps.Elements("ModId"))
                depsArray.Add(e.Value);
        }

        XElement nameElem = root.Element("Name") 
            ?? throw new Exception($"Mod at {path}, 'about.xml' file is missing <Name> element.");
        XElement modIdElem = root.Element("ModId") 
            ?? throw new Exception($"Mod at {path}, 'about.xml' file is missing <ModId> element.");
        XElement versionElem = root.Element("Version") 
            ?? throw new Exception($"Mod at {path}, 'about.xml' file is missing <Version> element.");

        string name = nameElem.Value;
        string modId = modIdElem.Value;
        string version = versionElem.Value;

        string description = root.Element("Description")?.Value;

        return new ModInfo(
            name,
            modId,
            version,
            description,
            depsArray.ToArray()
        );
    }
}
