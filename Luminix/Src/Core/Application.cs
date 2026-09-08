using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Luminix.Threading;

public static class Application
{
    public static Assembly Assembly { get; }
    public static string BuildVers { get; }
    public static string BuildDate { get; }
    
    public static string OSDescription{ get; } = RuntimeInformation.OSDescription;
    public static Architecture ProcessArchitecture { get; } = RuntimeInformation.ProcessArchitecture;
    public static Architecture OSArchitecture { get; } = RuntimeInformation.OSArchitecture;
    public static string FrameworkDescription { get; } = RuntimeInformation.FrameworkDescription; // e.g., ".NET 8.0.0"
    public static string MachineName { get; } = Environment.MachineName;
    public static int ProcessorCount { get; } = Environment.ProcessorCount;

    public static bool IsWindows { get; } = OperatingSystem.IsWindows();
    public static bool IsMacOS { get; } = OperatingSystem.IsMacOS();
    public static bool IsLinux { get; } = OperatingSystem.IsLinux();

    private static DateTime startTime { get; }
    public static TimeSpan Uptime => DateTime.UtcNow - startTime;
    public static string ExecutablePath => AppContext.BaseDirectory;

    private static string[] namespaces;
    public static ReadOnlySpan<string> Namespaces => namespaces.AsSpan();

    static Application()
    {
        Assembly = Assembly.GetExecutingAssembly();

        string version = Assembly.GetName().Version.ToString();
        BuildVers = version;

        var attribute = Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                            .FirstOrDefault(a => a.Key == "BuildDate");
        string buildDate = attribute?.Value ?? "Unknown";
        BuildDate = buildDate;

        var myNamespaces = Assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(n => !string.IsNullOrEmpty(n) && n.StartsWith("Luminix"))
            .Distinct()
            .OrderBy(n => n);

        namespaces = myNamespaces.ToArray();

        startTime = DateTime.UtcNow;
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

    public static void Restart() => throw new NotImplementedException(nameof(Restart));

    public static void Exit()
    {
        if (Thread.Threads != null)
            foreach (var thread in Thread.Threads)
                if(thread != Thread.CurrentThread)
                    thread?.Stop();
    }

    public static void ForceExit(int code)
    {
        Environment.Exit(code);
    }
}