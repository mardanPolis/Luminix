using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public sealed class Screen
{
    internal Screen(GraphicsAdapter adapter)
    {
        Name = adapter.DisplayName;
        //Description = adapter.Description;
        IsWideScreen = adapter.IsWideScreen;

        resolutions = new Graphics.Resolution[adapter.SupportedDisplayModes.Length];
        for(int modeIndex = 0; modeIndex < adapter.SupportedDisplayModes.Length; modeIndex++)
        {
            var mode = adapter.SupportedDisplayModes[modeIndex];
            resolutions[modeIndex] = new Graphics.Resolution(new Point(mode.Width, mode.Height), mode.RefreshRate);
        }
    }

    public string Name { get; }
    public string Description { get; }
    public bool IsWideScreen { get; }
    private readonly Graphics.Resolution[] resolutions;
    public ReadOnlySpan<Graphics.Resolution> Resolutions => resolutions;
}