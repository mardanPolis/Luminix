using System;

namespace Verse;

public static class Settings
{
    internal sealed class Controller
    {
        private Controller() { }
        internal static Controller Create()
        {
            if(controller != null)
                throw new System.InvalidOperationException("Controller has already been initialized.");

            return new Controller();
        }
        public bool Debug { set => Settings.Debug = value; }
        public int PixelsPerUnit { set => Settings.PixelsPerUnit = value; }
    }
    private static Controller controller;
    internal static void Initialize(out Controller controllerInstance)
    {
        controller = Controller.Create();
        controllerInstance = controller;
    }

    public static bool Debug { get; private set; }
    public static bool DebugInfo { get; set; }
    public static bool DebugGizmos { get; set; }

    public static int PixelsPerUnit { get; private set; } = 32;
}
