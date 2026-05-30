using Microsoft.Xna.Framework;

namespace Luminix.Resources.BuiltIn;

internal static class BuiltInResources
{
    public static Color[] NullTextureColor
    {
        get
        {
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = ((x / 16 ^ y / 16) & 1) == 1
                        ? Color.Magenta
                        : Color.Black;
            return pixels;
        }
    }
}