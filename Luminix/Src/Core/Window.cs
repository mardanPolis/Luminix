using System;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class Window
{
    internal Window() {}

    public enum Mode
    {
        Windowed,
        Fullscreen
    }

    internal IntPtr Handle { get; set; }

    public string Title { get; set; }
    public Point Position { get; set; }
    public Mode CurrentMode { get; set; }

    public Rectangle ClientBounds { get; internal set; }
    public bool IsFocused { get; internal set; }
    public bool IsMinimized { get; internal set; }
}