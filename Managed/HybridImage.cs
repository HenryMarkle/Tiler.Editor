namespace Tiler.Editor.Managed;

using System;
using Raylib_cs;

/// <summary>
/// Can live in either RAM or VRAM
/// </summary>
public class HybridImage : IDisposable
{
    public Raylib_cs.Image Image;
    public Texture2D Texture;

    public bool IsLoadedToVRAM { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public static implicit operator Raylib_cs.Image(HybridImage h) => h.IsLoadedToVRAM 
        ? throw new InvalidOperationException("Image is loaded to VRAM; can't be used as a CPU image") 
        : h.Image;
    public static implicit operator Texture2D(HybridImage h) => !h.IsLoadedToVRAM 
        ? throw new InvalidOperationException("Image is not loaded to VRAM; can't be used as a GPU image") 
        : h.Texture;

    public HybridImage(Raylib_cs.Image image)
    {
        Image = image;
        Width = image.Width;
        Height = image.Height;
    }

    public HybridImage(Texture2D texture)
    {
        Texture = texture;
        IsLoadedToVRAM = true;
        Width = texture.Width;
        Height = texture.Height;
    }

    public void ToTexture()
    {
        if (IsLoadedToVRAM) return;

        Texture = Raylib.LoadTextureFromImage(Image);
        Raylib.UnloadImage(Image);

        IsLoadedToVRAM = true;
    }

    public void ToImage()
    {
        if (!IsLoadedToVRAM) return;

        Image = Raylib.LoadImageFromTexture(Texture);
        Unloader.Enqueue(Texture);

        IsLoadedToVRAM = false;
    }

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        if (IsLoadedToVRAM) Unloader.Enqueue(Texture);
        else Raylib.UnloadImage(Image);
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~HybridImage()
    {
        Dispose();
    }
}