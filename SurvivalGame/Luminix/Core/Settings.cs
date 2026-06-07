using System;
using System.Runtime.Serialization;
using Luminix.Collections;

namespace Luminix;

public static class Settings
{
    internal sealed class Controller
    {
        public Controller()
        {
            if(Settings.initialized)
                throw new ArgumentException("Cant create a Settings.Controller, settings class is already initialized!");
        }

        public bool Debug { set => Settings.debug = value; }
        public bool DebugInitialization { set => Settings.debugInitialization = value; }
        public bool DebugAssets { set => Settings.debugAssets = value; }
        public int PixelsPerUnit { set => Settings.pixelsPerUnit = value; }
        public int MaxWorkerThreadCount { set => Settings.maxWorkerThreadCount = value; }
        public int MinWorkerThreadCount { set => Settings.minWorkerThreadCount = value; }
    }
    private static volatile bool initialized;
    
    private static bool debug;
    private static bool debugInitialization;
    private static bool debugAssets;
    private static volatile bool debugInfo;
    private static volatile bool debugConsole;
    private static volatile bool debugInspector;
    private static volatile bool debugGizmoPanel;
    private static volatile bool debugEventPanel;
    private static volatile int pixelsPerUnit = 32;
    private static volatile int maxWorkerThreadCount = 5;
    private static volatile int minWorkerThreadCount = 2;
    private static volatile int screenshotWidth = 1920;
    private static volatile int screenshotHeight = 1080;

    public static bool Debug { get => debug; }
    public static bool DebugInitialization { get => debugInitialization; }
    public static bool DebugAssets { get => debugAssets; }
    public static bool DebugInfo { get => debugInfo; set => debugInfo = value; }
    public static bool DebugConsole { get => debugConsole; set => debugConsole = value; }
    public static bool DebugInspector { get => debugInspector; set => debugInspector = value; }
    public static bool DebugGizmosPanel { get => debugGizmoPanel; set => debugGizmoPanel = value; }
    public static bool DebugEventPanel { get => debugEventPanel; set => debugEventPanel = value; }
    public static int PixelsPerUnit { get => pixelsPerUnit; }
    public static int MaxWorkerThreadCount { get => maxWorkerThreadCount; }
    public static int MinWorkerThreadCount { get => minWorkerThreadCount; }

    public static int ScreenshotWidth { get => screenshotWidth; }
    public static int ScreenshotHeight { get => screenshotHeight; }

    private static volatile bool finalized;
    private static Flags options { get; } = new Flags();


    internal static void Initialize(out Controller controller)
    {
        controller = new Controller();
        initialized = true;
    }

    internal static void FinalizeOptions()
    {
        finalized = true;
    }

    public static bool HasOption(string option) => options.HasFlag(option);
    public static bool? GetOption(string option) => options.GetFlag(option);
    public static void AddOption(string option, bool value)
    {
        if(finalized)
            throw new InvalidOperationException("Cant add options after settings have been finalized!");

        options.SetFlag(option, value);
    }

}
