using System;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

internal struct WindowSyncCommand
{
    public required IntPtr Handle;
    public required Point Position;
    public required Rectangle ClientBound;
    public required bool IsMinimized;
    public required bool IsFocused;
} 