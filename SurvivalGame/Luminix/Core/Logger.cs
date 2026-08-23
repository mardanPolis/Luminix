using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Luminix;

//FIXME : logger creates garbage, A LOT!

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
    private static string GetThreadName()
    {
        string name = Thread.CurrentThread.Name;
        if(string.IsNullOrWhiteSpace(name))
            return "";
        
        return $"[{name}]";
    }

    private static string GetPrefixMessage(object prefix)
    {
        if(prefix == null)
            return "";
        string name = prefix.ToString();
        if(string.IsNullOrWhiteSpace(name))
            return "";
        return $"{name}";
    }

    public static Log New(object message, Log.Type type, bool timestamp, object prefix = null)
    {
        if(message == null)
            message = "NULL";

        var entry = "";

        string prefixMsg = GetPrefixMessage(prefix);
        if(!string.IsNullOrEmpty(prefixMsg))
            entry += $"{prefixMsg}";

        if(type != Log.Type.Print)
        {
            if(timestamp)
                entry += $"{GetTimestamp()} ";
            entry += $"{GetThreadName()} ";

            entry += $"{GetTypeMessage(type)} ";
        }

        return new Log(entry + message.ToString());
    }

    public override string ToString() => entry;
}

public sealed class Logger
{
    private sealed class PrefixScope : IDisposable
    {
        private readonly Logger owner;
        private readonly PrefixScope previous;
        private readonly object message;
        private bool disposed;

        public PrefixScope(Logger owner, PrefixScope previous, object message)
        {
            this.owner = owner;
            this.previous = previous;
            this.message = message;
        }

        public void Dispose()
        {
            if(disposed) return;
            disposed = true;
            owner.prefix = previous;
        }

        public override string ToString()
        {
            if(previous == null)
                return message.ToString();

            if(previous.disposed)
                return null;
            
            return previous.ToString() + message.ToString();
        }
    }

    private static readonly ThreadLocal<Logger> instance = new ThreadLocal<Logger>(true);

    internal static Logger Initialize()
    {
        if(instance.IsValueCreated)
            return instance.Value;

        var logger = new Logger();
        instance.Value = logger;
        return logger;
    }
    public static Logger Get()
    {
        return instance.Value;
    }
    public static Logger[] GetAll()
    {
        return instance.Values.ToArray();
    }

    internal static void Shutdown()
    {
        var logger = instance.Value;
        if(logger != null)
        {
            logger.Flush();
            instance.Value = null;
        }
    }


    private Logger()
    {
        logs = new(10000);
        logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        
        path = GetThreadLogPath();
        File.WriteAllText(path, string.Empty);
    }

    public Log[] Logs
    {
        get
        {
            return logs.ToArray();
        }
    }

    private readonly string logDir;
    private readonly List<Log> logs;
    private readonly string path;

    private PrefixScope prefix;

    public IDisposable WithPrefix(object message)
    {
        var prefixScope = new PrefixScope(this, this.prefix, message);
        this.prefix = prefixScope;
        return prefixScope;
    }

    private void Print(Log log)
    {
        Console.WriteLine(log);
        logs.Add(log);
    }
    public void Print(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Print, false, prefix));
    }
    public void Log(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Info, true, prefix));
    }
    public void Log(object variable, object value)
    {
        Print(Luminix.Log.New($"{variable} : {value}", Luminix.Log.Type.Info, true, prefix));
    }
    public bool LogIf(bool statement, object message)
    {
        if(statement)
            Print(Luminix.Log.New(message, Luminix.Log.Type.Info, true, prefix));
        return statement;
    }

    public void Wrn(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Warning, true, prefix));
    }
    public bool WrnIf(bool statement, object message)
    {
        if(statement)
            Print(Luminix.Log.New(message, Luminix.Log.Type.Warning, true, prefix));
        return statement;
    }
    public void Err(object message)
    {
        Print(Luminix.Log.New(message, Luminix.Log.Type.Error, true, prefix));
    }
    public bool ErrIf(bool statement, object message)
    {
        if(statement)
            Print(Luminix.Log.New(message, Luminix.Log.Type.Error, true, prefix));
        return statement;
    }

    public void Flush()
    {
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

    private string GetThreadLogPath()
    {
        string name = Thread.CurrentThread.Name;
        return Path.Combine(
            logDir,
            $"thread_{name}.log"
        );
    }
}
