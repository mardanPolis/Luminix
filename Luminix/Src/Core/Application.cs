using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Luminix;
using Luminix.Threading;

public sealed class Application
{
    internal static Application Instance { get; private set; }

    public static Assembly EngineAssembly { get; }
    public static Assembly GameAssembly { get; }
    public static string BuildVers { get; }
    public static string BuildDate { get; }

    public static string Name => Instance.name;
    public static string Author => Instance.author;
    public static Version Version => Instance.version;
    public static bool IsModdable => Instance.moddable;

    public static string OSDescription{ get; } = RuntimeInformation.OSDescription;
    public static Architecture ProcessArchitecture { get; } = RuntimeInformation.ProcessArchitecture;
    public static Architecture OSArchitecture { get; } = RuntimeInformation.OSArchitecture;
    public static string FrameworkDescription { get; } = RuntimeInformation.FrameworkDescription; // e.g., ".NET 8.0.0"
    public static string MachineName { get; } = Environment.MachineName;
    public static int ProcessorCount { get; } = Environment.ProcessorCount;

    public static bool IsWindows { get; } = OperatingSystem.IsWindows();
    public static bool IsMacOS { get; } = OperatingSystem.IsMacOS();
    public static bool IsLinux { get; } = OperatingSystem.IsLinux();

    public static string ExecutablePath => AppContext.BaseDirectory;
    public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name);

    public static DateTime StartTime { get; }
    public static TimeSpan Uptime => DateTime.UtcNow - StartTime;

    public static ReadOnlySpan<string> Namespaces => Instance.namespaces.AsSpan();

    private readonly string name;
    private readonly string author;
    private readonly Version version;

    private readonly bool moddable;

    private string[] namespaces;

    private TickedThread coreThread;
    private Thread renderThread;

    static Application()
    {
        EngineAssembly = Assembly.GetExecutingAssembly();
        GameAssembly = Assembly.GetEntryAssembly();

        string version = EngineAssembly.GetName().Version.ToString();
        BuildVers = version;

        var attribute = EngineAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(a => a.Key == "BuildDate");
        string buildDate = attribute?.Value ?? "Unknown";
        BuildDate = buildDate;

        StartTime = DateTime.UtcNow;
    }

    public static Assembly GetAssembly(string name)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for(int i = 0; i < assemblies.Length; i++)
        {
            if(string.Equals(assemblies[i].GetName().Name, name, StringComparison.Ordinal))
            {
                return assemblies[i];
            }
        }

        throw new ArgumentException($"There is no loaded Assembly named '{name}'", nameof(name));
    }

    public Application(string name, string author, Version version, bool moddable, bool debug = false)
    {
        if (Instance != null)
            throw new InvalidOperationException("An Application instance already exists.");

        Logger.EchoToConsole = debug;

        Instance = this;
        this.name = name;
        this.author = author;
        this.version = version;
        this.moddable = moddable;

        var myNamespaces = EngineAssembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(n => !string.IsNullOrEmpty(n) && n.StartsWith("Luminix"))
            .Distinct()
            .OrderBy(n => n);

        namespaces = myNamespaces.ToArray();

        try
        {
            var mainCore = new Luminix.MainCore();
            coreThread = new TickedThread("CoreThread", mainCore.Initialize, mainCore.Tick, mainCore.OnStop);
            coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;

            var mainRender = new Luminix.MainRender();
            renderThread = new UnmanagedThread("RenderThread", mainRender.Run, mainRender.Stop);
            renderThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        }
        catch (Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Application failed to initialize: {ex}");
            throw;
        }
    }

    public void Run()
    {
        coreThread.Start();
        renderThread.Start();
    }

    public static void Restart() => throw new NotImplementedException(nameof(Restart));
    public static void Exit()
    {
        if (Thread.Threads != null)
            foreach (var thread in Thread.Threads)
                thread?.Stop();
    }

    public static void ForceStop(int code)
    {
        Environment.Exit(code);
    }
}