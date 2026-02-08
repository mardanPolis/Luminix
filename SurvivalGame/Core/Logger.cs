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
            
            entry += $"{GetTypeMessage(type)} : {message}";
        }

        return new Log(entry + " : " + message.ToString());
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

    private void Print(Log log)
    {
        Console.WriteLine(log);
        string path = GetThreadLogPath();
        File.AppendAllText(path, log + Environment.NewLine);
        logs.Add(log);
    }
    public void Print(object message)
    {
        Print(Core.Log.New(message, Core.Log.Type.Print, false));
    }
    public void Log(object message)
    {
        Print(Core.Log.New(message, Core.Log.Type.Info, true));
    }
    public void Log(object variable, object value)
    {
        Print(Core.Log.New($"{variable} : {value}", Core.Log.Type.Info, true));
    }
    public void Wrn(object message)
    {
        Print(Core.Log.New(message, Core.Log.Type.Warning, true));
    }
    public void Err(object message)
    {
        Print(Core.Log.New(message, Core.Log.Type.Error, true));
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