using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

internal enum WindowCommandType
{
    SetTitle,
    SetPosition,
    SetMode
}

internal struct WindowCommand
{
    public required WindowCommandType Kind;
    public string Title;
    public Point Position;
    public Window.Mode WindowMode;
}