using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Luminix.UI;

public class UIManager : Workspace.Service
{
    internal readonly struct Controller
    {
        internal Controller(UIManager controlled, Graphics graphics)
        {
            if(controlled.controllerIssued)
                throw new ArgumentException("Cant create a UIManager.Controller, the provided UI manager is already initialized!");
            
            this.controlled = controlled;
        }

        private readonly UIManager controlled;
        private readonly Graphics graphics;

        internal bool isMouseColliding(int mouseX, int mouseY)
        {
            controlled.handling = true;

            var collided = false;
            var first = true;
            var uiColliders = CollectionsMarshal.AsSpan(controlled.uiColliders);
            var count = uiColliders.Length;

            for (int i = 0; i < count; i++)
            {
                ref UICollider uiCollider = ref uiColliders[i];

                if (mouseX >= uiCollider.X && mouseX < uiCollider.X + uiCollider.Width &&
                    mouseY >= uiCollider.Y && mouseY < uiCollider.Y + uiCollider.Height)
                {
                    uiCollider.OnHit?.Invoke(first);
                    collided = true;
                    first = false;
                }
            }

            return collided;
        }

        internal void Render()
        {
            var uiElements = CollectionsMarshal.AsSpan(controlled.uiElements);
            var count = uiElements.Length;

            for (int i = 0; i < count; i++)
            {
                ref UIElement uiElement = ref uiElements[i];
                graphics.DrawImage(uiElement.Texture, uiElement.X, uiElement.Y, uiElement.Width, uiElement.Height);
            }
        }

        internal void Clear()
        {
            controlled.handling = false;

            controlled.uiColliders.Clear();
            controlled.uiElements.Clear();
        }
    }
    private readonly record struct UIElement(TextureResource Texture, int X, int Y, int Width, int Height);
    private readonly record struct UICollider(int X, int Y, int Width, int Height, Action<bool> OnHit);

    internal UIManager(Graphics graphics, out Controller controller)
    {
        this.controllerIssued = false;
        controller = new Controller(this, graphics);
        this.controllerIssued = true;

        uiElements  = new(128);
        uiColliders = new(128);

        //PixelTexture = Workspace.GetService<ResourceProvider>().GetResource<TextureResource>("Pixel");
    }

    private readonly TextureResource PixelTexture;

    private readonly bool controllerIssued;
    private bool handling;

    private List<UIElement> uiElements;
    private List<UICollider> uiColliders;

    public void NewImage(TextureResource texture, int x, int y, int width, int height)
    {
        if(handling)
        {
            logger.Wrn("Unable to create a UI element (Image), at this stage!");
            return;
        }

        var textureResource = texture ?? PixelTexture;
        uiElements.Add(new UIElement(textureResource, x, y, width, height));
    }

    public void NewCollider(int x, int y, int width, int height, Action<bool> onHit = null)
    {
        if(handling)
        {
            logger.Wrn("Unable to create a UI element (Collider), at this stage!");
            return;
        }

        uiColliders.Add(new UICollider(x, y, width, height, onHit));
    }
}