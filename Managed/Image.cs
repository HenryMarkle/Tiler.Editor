namespace Tiler.Editor.Managed;

using System;
using Raylib_cs;

public class Image(Raylib_cs.Image image) : IDisposable
{
    public Raylib_cs.Image Raw = image;
    public int Width => Raw.Width;
    public int Height => Raw.Height;

    public static implicit operator Raylib_cs.Image(Image i) => i.Raw;

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        Raylib.UnloadImage(Raw);
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~Image()
    {
        Dispose();
    }
}