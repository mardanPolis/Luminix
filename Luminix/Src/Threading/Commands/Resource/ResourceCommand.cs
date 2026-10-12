using System;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Commands;

internal enum GraphicResourceCommandType : byte
{
    Create = 0,
    Update = 1,
    Unload = 2,
    Link = 3,
    Unlink = 4
}

internal struct GraphicResourceCommand
{
    public GraphicResourceCommand()
    {
        this = default;
    }

    // --- TEXTURE ---
    public static GraphicResourceCommand Create(uint id, Color[] colors, int width, int height)
    {
        return New(GraphicResourceCommandType.Create, id, colors, width, height);
    }
    public static GraphicResourceCommand Update(uint id, Color[] colors, int width, int height)
    {
        return New(GraphicResourceCommandType.Update, id, colors, width, height);
    }
    private static GraphicResourceCommand New(GraphicResourceCommandType commandType, uint id, Color[] colors, int width, int height)
    {
        return new GraphicResourceCommand()
        {
            CommandType = commandType,
            ResourceType = GraphicResourceType.Texture,
            id = id,
            Data = colors,
            Width = width,
            Height = height,
        };
    }

    // --- SHADER / FONT ---
    public static GraphicResourceCommand Create(uint id, byte[] data)
    {
        return New(GraphicResourceCommandType.Create, id, data);
    }
    private static GraphicResourceCommand New(GraphicResourceCommandType commandType, uint id, byte[] data)
    {
        return new GraphicResourceCommand()
        {
            CommandType = commandType,
            ResourceType = GraphicResourceType.Shader,
            id = id,
            Data = data,
        };
    }

    public static GraphicResourceCommand Unload(uint id, GraphicResourceType resourceType)
    {
        return new GraphicResourceCommand()
        {
            CommandType = GraphicResourceCommandType.Unload,
            ResourceType = resourceType,
            id = id,
        };
    }

    public GraphicResourceCommandType CommandType { get; init; } // 0 - create, 1 - update, 2 - unload, 3 - link
    public  GraphicResourceType ResourceType { get; init; }

    public uint id { get; init; }
    public object Data { get; init; }

    public int Width { get; init; }
    public int Height { get; init; }
}