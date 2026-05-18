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

        public bool Debug { set => Settings.Debug = value; }
        public int PixelsPerUnit { set => Settings.PixelsPerUnit = value; }
        public int MaxWorkerThreadCount { set => Settings.MaxWorkerThreadCount = value; }
        public int MinWorkerThreadCount { set => Settings.MinWorkerThreadCount = value; }
    }
    private static volatile bool initialized;

    public static bool Debug { get; private set; }
    public static bool DebugInfo { get; set; }
    public static bool DebugGizmos { get; set; }

    public static int PixelsPerUnit { get; private set; } = 32;

    public static int MaxWorkerThreadCount { get; private set; } = 5;
    public static int MinWorkerThreadCount { get; private set; } = 5;

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
