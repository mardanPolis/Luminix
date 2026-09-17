using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Luminix.Threading;

namespace Luminix;

public sealed class Application
{
    internal static Application Instance { get; private set; }

    internal const string CORE_THREAD_NAME = "CoreThread";
    internal const string RENDER_THREAD_NAME = "RenderThread";

    public static Assembly EngineAssembly { get; }
    public static Assembly GameAssembly { get; }
    public static string BuildVers { get; }
    public static string BuildDate { get; }

    public static string Name => Instance.name;
    public static string Author => Instance.author;
    public static Version Version => Instance.version;
    public static bool IsModdable => Instance.moddable;
    public static bool IsDebug => Instance.isDebug;

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
    public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameAssembly.GetName().Name);

    public static DateTime StartTime { get; }
    public static TimeSpan Uptime => DateTime.UtcNow - StartTime;

    private static string[] namespaces;
    public static ReadOnlySpan<string> EngineNamespaces => namespaces.AsSpan();

    private readonly string name;
    private readonly string author;
    private readonly Version version;

    private readonly bool moddable;

    private readonly bool isDebug;

    private TickedThread coreThread;
    private Thread renderThread;

    static Application()
    {
        EngineAssembly = Assembly.GetExecutingAssembly();
        GameAssembly = Assembly.GetEntryAssembly();

        string version = EngineAssembly.GetName().Version.ToString();
        BuildVers = version;

        string buildDate = EngineAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "Unknown";
        BuildDate = buildDate;

        StartTime = DateTime.UtcNow;

        var myNamespaces = EngineAssembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(n => !string.IsNullOrEmpty(n) && n.StartsWith(EngineAssembly.GetName().Name))
            .Distinct()
            .OrderBy(n => n);

        namespaces = myNamespaces.ToArray();
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

    public Application(string name, string author, Version version, bool moddable, bool isDebug = false)
    {
        {
            if (Instance != null)
                throw new InvalidOperationException("An Application instance already exists.");

            Instance = this;
            this.name = name;
            this.author = author;
            this.version = version;
            this.moddable = moddable;
            this.isDebug = isDebug;
        }

    }

    public void Run()
    {
        MainCore mainCore = null;
        MainRender mainRender = null;

        try
        {
            mainCore = new Luminix.MainCore();
            coreThread = new TickedThread(CORE_THREAD_NAME, mainCore.Initialize, mainCore.Tick, mainCore.OnStop);
            coreThread.Priority = System.Threading.ThreadPriority.AboveNormal;

            mainRender = new Luminix.MainRender();
            renderThread = new UnmanagedThread(RENDER_THREAD_NAME, mainRender.Run, mainRender.Stop);
            renderThread.Priority = System.Threading.ThreadPriority.AboveNormal;

            coreThread.Start();
            renderThread.Start();
        }
        catch (Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Application encountered an Error: {ex}");
            throw;
        }
        finally
        {
            mainRender.Dispose();
        }
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