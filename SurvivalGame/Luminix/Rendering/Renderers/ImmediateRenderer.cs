using Luminix.Resources;
using Luminix.Workspaces;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Rendering;

public abstract class ImmediateRenderer
{
    protected const string MATRIX_PARAMETER = "MatrixTransform";
    protected const string TEXTURE_PARAMETER = "Texture";
    protected const string TEXTURE_SIZE_PARAMETER = "TextureSize";

    public ImmediateRenderer()
    {
        renderingSystem = Workspace.Current.GetService<RenderingSystem>();
        resourceProvider = renderingSystem.ResourceManager.ResourceProvider;
        spriteBatch = renderingSystem.SpriteBatch;

        defaultShader = resourceProvider.GetResource<ShaderResource>("GenericShader");
        defaultTexture = resourceProvider.GetResource<TextureResource>("NullTexture");
    }

    protected readonly RenderingSystem  renderingSystem;
    protected readonly ResourceProvider resourceProvider;
    protected readonly SpriteBatch      spriteBatch;

    protected readonly ShaderResource  defaultShader;
    protected readonly TextureResource defaultTexture;
    public abstract void Render();

}