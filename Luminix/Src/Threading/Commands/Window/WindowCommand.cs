using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

internal struct WindowCommand
{
    public enum Type { SetTitle, SetPosition, SetMode }
    public Type Kind;
    public string Title;
    public Point Position;
    public Window.Mode WindowMode;
}