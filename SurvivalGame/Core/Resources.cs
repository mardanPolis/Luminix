using System;
using Microsoft.Xna.Framework.Graphics;

namespace SurvivalGame.Core;

public static class Resources
{
    public static Texture2D LoadTexture(string path)
    {
        return SurvivalGame.Instance.Content.Load<Texture2D>(path);
    }
}
