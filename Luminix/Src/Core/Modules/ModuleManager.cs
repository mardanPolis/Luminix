using System;
using System.Collections.Generic;
using System.IO;
using Luminix.Exceptions;
using Luminix.Json;

namespace Luminix;

public sealed class ModuleManager : Workspace.Service
{
    private Dictionary<string, Module> initializedModules;
    private List<ModuleAbout> modules;

    protected override void Start()
    {
        initializedModules = new(StringComparer.OrdinalIgnoreCase);
        modules = [];

        string modulesRoot = Path.GetFullPath("modules", Application.ExecutablePath);
        if (!Directory.Exists(modulesRoot))
        {
            Directory.CreateDirectory(modulesRoot);
            return;
        }

        foreach (string moduleDirectory in Directory.GetDirectories(modulesRoot))
        {
            try
            {
                ModuleAbout about = LoadModuleAbout(moduleDirectory);
                Logger.NewLog(Logger.Log.Level.Info, $"Discovered module '{about.ID}' at '{moduleDirectory}'.");
            }
            catch (DuplicateModuleException ex)
            {
                Logger.NewLog(Logger.Log.Level.Warning, ex.Message);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to load module from '{moduleDirectory}': {ex.Message}");
            }
        }
    }

    private ModuleAbout LoadModuleAbout(string moduleDirectory)
    {
        moduleDirectory = Path.GetFullPath(moduleDirectory, Application.ExecutablePath);

        string aboutPath = Path.Combine(moduleDirectory, "about.json");
        if (!File.Exists(aboutPath))
            throw new InvalidModuleManifestException(moduleDirectory, "about.json not found.");

        string json = File.ReadAllText(aboutPath);
        ModuleAbout moduleAbout = JsonUtility.Deserialize<ModuleAbout>(json);

        if (moduleAbout is null || string.IsNullOrWhiteSpace(moduleAbout.ID))
            throw new InvalidModuleManifestException(moduleDirectory, "Manifest is missing a valid ID.");

        var moduleID = moduleAbout.ID.ToLower();

        if (modules.TryAdd(moduleID, moduleAbout))
            return moduleAbout;

        throw new DuplicateModuleException(moduleID, moduleDirectory);
    }

    public void InitializeModules(ReadOnlySpan<string> moduleIDs)
    {
        for (int i = 0; i < moduleIDs.Length; i++)
        {
            string moduleID = moduleIDs[i];

            if (!modules.TryGetValue(moduleID, out ModuleAbout moduleAbout))
            {
                Logger.NewLog(Logger.Log.Level.Warning, $"Cannot initialize unknown module '{moduleID}'.");
                continue;
            }

            if (initializedModules.ContainsKey(moduleID))
            {
                Logger.NewLog(Logger.Log.Level.Info, $"Module '{moduleID}' is already initialized; skipping.");
                continue;
            }

            try
            {
                Module module = null;

                initializedModules.Add(moduleID, module);
                Logger.NewLog(Logger.Log.Level.Info, $"Initialized module '{moduleID}'.");
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to initialize module '{moduleID}': {ex.Message}");
                throw new ModuleInitializationException(moduleID, ex);
            }
        }
    }

    public Module GetModule(string moduleID)
    {
        if (!initializedModules.TryGetValue(moduleID, out Module module))
            throw new ModuleNotInitializedException(moduleID);

        return module;
    }
}