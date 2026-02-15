using System.Runtime.CompilerServices;

namespace Tiler.Editor.Managed;

using System;
using System.Numerics;

using Raylib_cs;

/// <summary>
/// Can live in RAM, VRAM, or the disk.
/// </summary>
public class DynamicImage : IDisposable
{
    public string Path { get; init; }
    public Raylib_cs.Image Image { get; private set; }
    public Texture2D Texture { get; private set; }
    
    public enum StorageTypes { Disk, RAM, VRAM }
    public StorageTypes StorageType { get; private set; }

    public DynamicImage(string path)
    {
        (Width, Height) = RlUtils.GetImageSize(path);
        StorageType = StorageTypes.Disk;
        Path = path;
    }

    public DynamicImage(string path, Raylib_cs.Image image)
    {
        StorageType = StorageTypes.RAM;
        Image = image;
        Width = image.Width;
        Height = image.Height;
        Path = path;
    }

    public DynamicImage(string path, Texture2D texture)
    {
        StorageType = StorageTypes.VRAM;
        Texture = texture;
        Width = texture.Width;
        Height = texture.Height;
        Path = path;
    }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Vector2 Size => new(Width, Height);

    public static implicit operator Raylib_cs.Image(DynamicImage h) => h.StorageType is not StorageTypes.RAM 
        ? throw new InvalidOperationException("Image is loaded to VRAM; can't be used as a CPU image") 
        : h.Image;
    public static implicit operator Texture2D(DynamicImage h) => h.StorageType is not StorageTypes.VRAM 
        ? throw new InvalidOperationException("Image is not loaded to VRAM; can't be used as a GPU image") 
        : h.Texture;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unload()
    {
        switch (StorageType)
        {
            case StorageTypes.RAM:
                Raylib.UnloadImage(Image);
                break;
            
            case StorageTypes.VRAM:
                Raylib.UnloadTexture(Texture);
                break;
        }
        
        StorageType = StorageTypes.Disk;
    }

    public void ToDisk()
    {
        switch (StorageType)
        {
            case StorageTypes.Disk: return;
            
            case StorageTypes.RAM:
                Raylib.ExportImage(Image, Path);
                Raylib.UnloadImage(Image);
                break;

            case StorageTypes.VRAM:
                Image = Raylib.LoadImageFromTexture(Texture);
                Raylib.ExportImage(Image, Path);
                Raylib.UnloadImage(Image);
                Raylib.UnloadTexture(Texture);
                break;
            
            default: return;
        }

        StorageType = StorageTypes.Disk;
    }

    public void ToTexture()
    {
        switch (StorageType)
        {
            case StorageTypes.VRAM: return;
            
            case StorageTypes.RAM:
                Texture = Raylib.LoadTextureFromImage(Image);
                Raylib.UnloadImage(Image);
                break;
            
            case StorageTypes.Disk:
                Texture = Raylib.LoadTexture(Path);
                break;
            
            default: return;
        }

        StorageType = StorageTypes.VRAM;
    }

    public void ToImage()
    {
        switch (StorageType)
        {
            case  StorageTypes.RAM: return;
            
            case StorageTypes.VRAM:
                Image = Raylib.LoadImageFromTexture(Texture);
                Unloader.Enqueue(Texture);
                break;
            
            case StorageTypes.Disk:
                Image = Raylib.LoadImage(Path);
                break;
            
            default: return;
        }

        StorageType = StorageTypes.RAM;
    }

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        switch (StorageType)
        {
            case StorageTypes.RAM:
                Raylib.UnloadImage(Image);
                break;
            
            case StorageTypes.VRAM:
                Unloader.Enqueue(Texture);
                break;
        }
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~DynamicImage()
    {
        Dispose();
    }
}