using System;

namespace Verse;

public static class Settings
{
    internal sealed class Controller
    {
        public Controller()
        {
            if(Settings.initialized)
                throw new ArgumentException("Cant create a Settings.Controller, settings class is already initialized!");
        }

        public bool Debug { set => Settings.Debug = value; }
        public int PixelsPerUnit { set => Settings.PixelsPerUnit = value; }
    }
    internal static void Initialize(out Controller controller)
    {
        controller = new Controller();
        initialized = true;
    }

    private static bool initialized;

    public static bool Debug { get; private set; }
    public static bool DebugInfo { get; set; }
    public static bool DebugGizmos { get; set; }

    public static int PixelsPerUnit { get; private set; } = 32;
}
