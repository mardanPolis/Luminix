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
    public static bool DebugInitialization { get => debug && debugInitialization; }
    public static bool DebugAssets { get => debug && debugAssets; }
    public static bool DebugInfo { get => debug && debugInfo; set => debugInfo = value; }
    public static bool DebugConsole { get => debug && debugConsole; set => debugConsole = value; }
    public static bool DebugInspector { get => debug && debugInspector; set => debugInspector = value; }
    public static bool DebugGizmosPanel { get => debug && debugGizmoPanel; set => debugGizmoPanel = value; }
    public static bool DebugEventPanel { get => debug && debugEventPanel; set => debugEventPanel = value; }
    public static int PixelsPerUnit { get => pixelsPerUnit; }
    public static int MaxWorkerThreadCount { get => maxWorkerThreadCount; }
    public static int MinWorkerThreadCount { get => minWorkerThreadCount; }

    public static int ScreenshotWidth { get => screenshotWidth; }
    public static int ScreenshotHeight { get => screenshotHeight; }

    internal static void Initialize(out Controller controller)
    {
        controller = new Controller();
        initialized = true;
    }
}
