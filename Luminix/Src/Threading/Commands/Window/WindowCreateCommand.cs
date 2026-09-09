using System;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

public struct WindowCreateCommand
{
    public string Title;
    public Point? Position;
    public Window.Mode? WindowMode;
}

public struct WindowCreateCommandResponse
{
    public required IntPtr Handle;
    public required Point Position;
    public required Window.Mode WindowMode;
}