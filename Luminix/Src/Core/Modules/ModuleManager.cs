using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Luminix.Collections;
using Luminix.Exceptions;
using Luminix.Extensions;
using Luminix.Json;

namespace Luminix;

public sealed partial class ModuleManager : Workspace.Service
{
    private static readonly Action<ModuleBehavior> initializeBehavior;
    private static readonly Action<ModuleBehavior> startBehavior;
    private static readonly Action<ModuleBehavior> shutdownBehavior;

    private static readonly string modulesRoot;

    private static string InitialModulePath => Path.Combine(Application.DataPath, "modules.json");

    private readonly DenseMap<string, Module> modules;

    private HashSet<string> initializedModules;
    private DenseList<string> initialModules;

    private readonly Module coreModule;

    public ModuleEnumerable Modules => new(this);
    public InitializedModuleEnumerable InitializedModules => new(this);
    public NonInitialModuleEnumerable NonInitialModules => new(this);
    public InitialModuleEnumerable InitialModules => new(this);

    public int ModuleCount => modules.Count;
    public int InitializedModuleCount => initializedModules.Count;
    public int NonInitialModuleCount => modules.Count - initialModules.Count;
    public int InitialModuleCount => initialModules.Count;

#region Constructors
    static ModuleManager()
    {
        var initializeMethod = typeof(ModuleBehavior).GetMethod(
            "Initialize", BindingFlags.NonPublic | BindingFlags.Instance);

        var startMethod = typeof(ModuleBehavior).GetMethod(
            "Start", BindingFlags.NonPublic | BindingFlags.Instance);

        var shutdownMethod = typeof(ModuleBehavior).GetMethod(
            "Shutdown", BindingFlags.NonPublic | BindingFlags.Instance);

        initializeBehavior = (Action<ModuleBehavior>)Delegate.CreateDelegate(typeof(Action<ModuleBehavior>), initializeMethod);
        startBehavior = (Action<ModuleBehavior>)Delegate.CreateDelegate(typeof(Action<ModuleBehavior>), startMethod);
        shutdownBehavior = (Action<ModuleBehavior>)Delegate.CreateDelegate(typeof(Action<ModuleBehavior>), shutdownMethod);

        modulesRoot = Path.GetFullPath("modules", Application.ExecutablePath);
    }

    public ModuleManager()
    {
        modules = new(leftComparer: StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(modulesRoot))
            Directory.CreateDirectory(modulesRoot);

        coreModule = new()
        {
            ID = Application.Author.ToLower() + "_" + Application.Name.ToLower(),
            Name = Application.Name,
            Version = Application.Version,
            Description = $"Core {Application.Name} content.",
            Assembly = Application.GameAssembly.GetName().Name
        };
        coreModule.SetAssembly(Application.GameAssembly);
        coreModule.SetModuleDirectory(Application.ExecutablePath);

        modules.Add(coreModule.ID, coreModule);
    }
#endregion

    public bool IsModuleInitialized(Module module) => initializedModules.Contains(module.ID);

    public Module GetModule(string moduleID)
    {
        if (!modules.TryGetItem(moduleID, out Module module))
            throw new ModuleNotFoundException(moduleID);
 
        return module;
    }
 
    public Module GetInitializedModule(string moduleID)
    {
        if (!modules.TryGetItem(moduleID, out Module module))
            throw new ModuleNotFoundException(moduleID);
 
        if (!IsModuleInitialized(module))
            throw new ModuleNotInitializedException(moduleID);
 
        return module;
    }





#region LoadModules
    internal void FindModules()
    {
        if(!Application.IsModdable)
            return;

        foreach (string moduleDirectory in Directory.GetDirectories(modulesRoot))
        {
            try
            {
                LoadModule(moduleDirectory);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to load module from '{moduleDirectory}': {ex.Message}");
            }
        }
    }

    private void LoadModule(string moduleDirectory)
    {
        moduleDirectory = Path.GetFullPath(moduleDirectory, Application.ExecutablePath);

        string aboutPath = Path.Combine(moduleDirectory, "about.json");
        if (!File.Exists(aboutPath))
            throw new InvalidModuleManifestException(moduleDirectory, "about.json not found.");

        string json = File.ReadAllText(aboutPath);
        Module module = JsonUtility.Deserialize<Module>(json);

        if (module is null)
            throw new InvalidModuleManifestException(moduleDirectory, "Manifest is invalid.");

        if (string.IsNullOrWhiteSpace(module.ID))
            throw new InvalidModuleManifestException(moduleDirectory, $"Manifest has invalid ID - '{module.ID}' is null, empty or consists only of white-space characters.");

        if (module.ID.HasWhitespace())
            throw new InvalidModuleManifestException(moduleDirectory, $"Manifest has invalid ID - '{module.ID}' has whitespaces.");

        module.SetModuleDirectory(moduleDirectory);

        if (!modules.TryAdd(module.ID, module))
            throw new DuplicateModuleException(module.ID, moduleDirectory);
    }
#endregion

#region InitializeModules

    // Get modules to initialize
    // check if they exist if not log
    // check if their dependencies exist if not log
 
    // if has errors stop initializing, but initialize the core module
 
    // loop over each module, load assembly and initialize
 
    internal void InitializeModules()
    {
        DenseList<string> moduleIDs = GetInitialModules();
 
        initializedModules = [];
        initialModules = [];

        if (!ValidModules(moduleIDs.AsSpan()))
        {
            initialModules.AddRange(moduleIDs);
            InitializeModule(coreModule);
            return;
        }

        foreach (var moduleID in moduleIDs)
            InitializeModule(modules[moduleID]);
    }

    private DenseList<string> GetInitialModules()
    {
        try
        {
            DenseList<string> toInitialize = [];
 
            var moduleConfigPath = InitialModulePath;
            if (File.Exists(moduleConfigPath))
            {
                string json = File.ReadAllText(moduleConfigPath);
                toInitialize = JsonUtility.Deserialize<DenseList<string>>(json) ?? toInitialize;
            }
 
            if(!toInitialize.Contains(coreModule.ID))
                toInitialize.Insert(0, coreModule.ID);

            return toInitialize;
        }
        catch (Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Failed to get initial modules, reason: {ex}");
            return [];
        }
    }
 
    private bool ValidModules(ReadOnlySpan<string> moduleIDs)
    {
        var success = true;
        var existingModules = new HashSet<string>();
 
        foreach (var moduleID in moduleIDs)
        {
            try
            {
                if (!modules.TryGetItem(moduleID, out Module module))
                {
                    Logger.NewLog(Logger.Log.Level.Error, new ModuleNotFoundException(moduleID).Message);
                    success = false;
                    continue;
                }
 
                existingModules.Add(moduleID);
 
                foreach (var dep in module.Dependencies)
                {
                    if (!existingModules.Contains(dep))
                    {
                        Logger.NewLog(Logger.Log.Level.Error, new ModuleDependencyException(module.ID, dep).Message);
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to check Module '{moduleID}', reason: {ex}.");
                success = false;
            }
        }
 
        return success;
    }
 
    private void InitializeModule(Module module)
    {
        try
        {
            if (IsModuleInitialized(module))
                return;
 
            if (!module.HasAssembly())
            {
                initializedModules.Add(module.ID);
                return;
            }
 
            if(!module.IsAssemblyLoaded())
            {
                Assembly assembly = Assembly.LoadFrom(Path.Combine(module.GetModuleDirectory(), module.GetAssemblyName() + ".dll"));
                module.SetAssembly(assembly);
            }

            if (module.TryGetModuleBehavior(module, out var behavior))
            {
                initializeBehavior(behavior);
            }

            initializedModules.Add(module.ID);
        }
        catch (Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, new ModuleInitializationException(module.ID, ex).Message);
        }
    }

#endregion

    protected override void Start()
    {
        foreach (var module in modules.RightItems)
        {
            try
            {
                if (IsModuleInitialized(module) && module.HasBehavior())
                {
                    var behavior = module.GetBehavior();
                    startBehavior(behavior);
                } 
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"{new ModuleStartException(module.ID, ex)}");
            }
        }
    }
    protected override void Shutdown()
    {
        foreach (var module in modules.RightItems)
        {
            try
            {
                if (IsModuleInitialized(module) && module.HasBehavior())
                {
                    var behavior = module.GetBehavior();
                    shutdownBehavior(behavior);
                }
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"{new ModuleShutdownException(module.ID, ex)}");
            }
        }
    }
}