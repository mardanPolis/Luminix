using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SurvivalGame.Core;

public readonly struct Log
{
    public enum Type
    {
        Log,
        Warning,
        Error,
    }

    public Log(object message, Log.Type type, string callerMember, string callerFile, int callerLine)
    {
        entry = CreateEntry(message, type, callerMember, callerFile, callerLine);
    }
    public Log(object message)
    {
        entry = message.ToString();
    }
    
    private readonly string entry;

    public static implicit operator string(Log log) => log.entry;

    private static string CreateEntry(object message, Log.Type type, string callerMember, string callerFile, int callerLine)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string typeMsg = type == Type.Log ? "" : type == Type.Warning ? "[WARNING]" : "[ERROR]";
        return $"[{timestamp}] [{Path.GetFileName(callerFile)}:{callerLine}] [{callerMember}] {typeMsg}: {message}";
    }
    public override string ToString() => entry;
}

public sealed class Logger
{
    private static readonly Logger _instance = new();
    private readonly string _logDir;
    private readonly List<Log> logs;
    private Logger()
    {
        logs = new();
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

    public void Print(object message)
    {
        Print(new Core.Log(message));
    }
    private void Print(Log log)
    {
        Console.WriteLine(log);
        string path = GetThreadLogPath();
        File.AppendAllText(path, log + Environment.NewLine);
        logs.Add(log);
    }

    public void Log(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Print(new Log(message, Core.Log.Type.Log, member, file, line));
    }
    public void Log(object variable, object value, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Print(new Log($"{variable} : {value}", Core.Log.Type.Log, member, file, line));
    }
    public void Wrn(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Print(new Log(message, Core.Log.Type.Warning, member, file, line));
    }
    public void Err(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        Print(new Log(message, Core.Log.Type.Error, member, file, line));
    }

    public void Clear()
    {
        Console.Clear();
        logs.Clear();
    }


    public Log[] Logs
    {
        get
        {
            return logs.ToArray();
        }
    }

    public static Logger GetLogger()
    {
        return _instance;
    }
}