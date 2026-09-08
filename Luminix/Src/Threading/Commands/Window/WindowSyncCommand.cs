using System;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

internal struct WindowSyncCommand
{
    public IntPtr Handle;
    public Point Position;
    public Rectangle ClientBound;
    public bool IsMinimized;
    public bool IsFocused;
} 