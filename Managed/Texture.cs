namespace Tiler.Editor.Managed;

using System;
using System.Numerics;
using Raylib_cs;

public class Texture(Texture2D texture) : IDisposable
{
    public Texture2D Raw = texture;
    public int Width => Raw.Width;
    public int Height => Raw.Height;
    public Vector2 Size => new(Width, Height);

    public Texture(Image image) : this(Raylib.LoadTextureFromImage(image)) {}

    public static implicit operator Texture2D(Texture t) => t.Raw;

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        Unloader.Enqueue(Raw);
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~Texture()
    {
        Dispose();
    }
}