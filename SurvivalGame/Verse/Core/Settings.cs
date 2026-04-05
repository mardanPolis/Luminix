using System;

namespace SurvivalGame.Verse.Core;

public sealed class Settings
{
    internal Settings() 
    {
        if(instance != null)
            throw new Exception("Settings were");
    }

    private static Settings instance;

    public bool Debug { get; set; }
    public bool DebugInfo { get; set; }
    public bool DebugGizmos { get; set; }
}
