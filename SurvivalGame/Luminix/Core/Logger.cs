using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Luminix;

public readonly struct Log
{
    public enum Type
    {
        Print,
        Info,
        Warning,
        Error,
    }

    private readonly string entry;

    private Log(string entry)
    {
        this.entry = entry;
    }

    public static implicit operator string(Log log) => log.entry;
    
    private static string GetTimestamp()
    {
        return $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}]";
    }
    private static string GetTypeMessage(Log.Type type)
    {
        switch(type)
        {
            case Log.Type.Info:
                return $"[INFO]";
            case Log.Type.Warning:
                return $"[WARNING]";
            case Log.Type.Error:
                return $"[ERROR]";
            default:
                return "";
        }
    }

    public static Log New(object message, Log.Type type, bool timestamp)
    {
        if(message == null)
            message = "NULL";

        var entry = "";

        if(type != Log.Type.Print)
        {
            if(timestamp)
                entry += $"{GetTimestamp()} ";
            
            entry += $"{GetTypeMessage(type)} ";
        }

        return new Log(entry + message.ToString());
    }

    public override string ToString() => entry;
}

public sealed class Logger
{
    private static readonly ThreadLocal<Logger> instance = new ThreadLocal<Logger>(true);

    private Logger()
    {
        logs = new();
        logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        
        string path = GetThreadLogPath();
        File.WriteAllText(path, string.Empty);
    }

    private readonly string logDir;
    private readonly List<Log> logs;
    
    public Log[] Logs
    {
        get
        {
            return logs.ToArray();
        }
    }

    private string GetThreadLogPath()
    {
        string name = Thread.CurrentThread.Name;
        return Path.Combine(
            logDir,
            $"thread_{name}.log"
        );
    }

    internal static void Initialize()
    {
        instance.Value = new Logger();
    }

    private void Print(Log log)
    {
        Console.WriteLine(log);
        logs.Add(log);
    }
    public void Print(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Print, false));
    }
    public void Log(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Info, true));
    }
    public void Log(object variable, object value)
    {
        Print(Luminix.Log.New($"{variable} : {value}", Luminix.Log.Type.Info, true));
    }
    public void Wrn(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Warning, true));
    }
    public void Err(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Error, true));
    }

    public void Flush()
    {
        string path = GetThreadLogPath();
        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            for (int i = 0; i < logs.Count; i++)
                writer.WriteLine(logs[i]);
        }
        logs.Clear();
    }

    public void Clear()
    {
        Console.Clear();
        logs.Clear();
    }

    public static Logger Get()
    {
        return instance.Value;
    }
    public static Logger[] GetAll()
    {
        return instance.Values.ToArray();
    }
}
