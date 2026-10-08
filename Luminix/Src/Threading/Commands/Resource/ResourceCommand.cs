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
    public static GraphicResourceCommand Create(string identifier, Color[] colors, int width, int height)
    {
        return New(GraphicResourceCommandType.Create, identifier, colors, width, height);
    }
    public static GraphicResourceCommand Update(string identifier, Color[] colors, int width, int height)
    {
        return New(GraphicResourceCommandType.Update, identifier, colors, width, height);
    }
    private static GraphicResourceCommand New(GraphicResourceCommandType commandType, string identifier, Color[] colors, int width, int height)
    {
        return new GraphicResourceCommand()
        {
            CommandType = commandType,
            ResourceType = GraphicResourceType.Texture,
            Identifier = identifier,
            Data = colors,
            Width = width,
            Height = height,
        };
    }

    // --- SHADER / FONT ---
    public static GraphicResourceCommand Create(string identifier, byte[] data, XnbAsset.XnbType xnbType)
    {
        return New(GraphicResourceCommandType.Create, identifier, data, xnbType);
    }
    private static GraphicResourceCommand New(GraphicResourceCommandType commandType, string identifier, byte[] data, XnbAsset.XnbType xnbType)
    {
        return new GraphicResourceCommand()
        {
            CommandType = commandType,
            ResourceType = xnbType switch
            {
                XnbAsset.XnbType.Font   => GraphicResourceType.Font,
                XnbAsset.XnbType.Shader => GraphicResourceType.Shader,
                _                       => throw new ArgumentOutOfRangeException(nameof(xnbType), $"Unsupported xnb type: {xnbType}")
            },
            Identifier = identifier,
            Data = data,
        };
    }

    public static GraphicResourceCommand Unload(string identifier, GraphicResourceType resourceType)
    {
        return new GraphicResourceCommand()
        {
            CommandType = GraphicResourceCommandType.Unload,
            ResourceType = resourceType,
            Identifier = identifier,
        };
    }

    public static GraphicResourceCommand Link(string sourceIdentifier, Resource resource)
    {
        return new GraphicResourceCommand()
        {
            CommandType = GraphicResourceCommandType.Link,
            ResourceType = resource switch
            {
                TextureResource => GraphicResourceType.Texture,
                FontResource    => GraphicResourceType.Font,
                ShaderResource  => GraphicResourceType.Shader,
                _               => throw new ArgumentOutOfRangeException(nameof(resource), $"Unsupported resource type: {resource.GetType()}")
            },
            Identifier = sourceIdentifier,
            Data = resource,
        };
    }
    public static GraphicResourceCommand Unlink(Resource resource)
    {
        return new GraphicResourceCommand()
        {
            CommandType = GraphicResourceCommandType.Unlink,
            Data = resource,
        };
    }

    public GraphicResourceCommandType CommandType { get; init; } // 0 - create, 1 - update, 2 - unload, 3 - link
    public  GraphicResourceType ResourceType { get; init; }

    public string Identifier { get; init; }
    public object Data { get; init; }

    public int Width { get; init; }
    public int Height { get; init; }
}