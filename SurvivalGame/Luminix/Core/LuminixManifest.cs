using System;
using System.Linq;
using System.Reflection;

public static class LuminixManifest
{
    public static Assembly Assembly { get; }
    public static string BuildVers { get; }
    public static string BuildDate { get; }
    
    private static string[] namespaces;
    public static ReadOnlySpan<string> Namespaces => namespaces.AsSpan();

    
    static LuminixManifest()
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
}