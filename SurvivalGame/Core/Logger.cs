using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SurvivalGame.Core;

public sealed class Logger
{
    private static readonly Logger _instance = new();
    private readonly string _logDir;
    private Logger()
    {
        _logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDir);
    }

    private static string GetThreadLogPath()
    {
        int id = Thread.CurrentThread.ManagedThreadId;
        return Path.Combine(
            AppContext.BaseDirectory,
            "logs",
            $"thread_{id}.log"
        );
    }

    public void Log(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string entry = $"[{timestamp}] [{Path.GetFileName(file)}:{line}] [{member}] {message}";

        Console.WriteLine(entry);
        
        string path = GetThreadLogPath();
        File.AppendAllText(path, entry + Environment.NewLine);
    }
    public void Wrn(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Log($"[WARNING] {message}", member:member, file:file, line:line);
    }

    
    public void Err(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Log($"[WARNING] {message}", member:member, file:file, line:line);
    }

    public void Log(string variable, object value, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Log($"{variable}: {value}", member:member, file:file, line:line);
    }

    public static Logger GetLogger()
    {
        return _instance;
    }
}