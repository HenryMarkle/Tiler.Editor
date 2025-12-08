namespace Tiler.Editor.Managed;

using System;
using Raylib_cs;

public class RenderTexture(RenderTexture2D rt) : IDisposable
{
    public RenderTexture2D Raw = rt;
    public int Width => Raw.Texture.Width;
    public int Height => Raw.Texture.Height;

    public Color4 ClearColor = new(255, 255, 255);

    public RenderTexture(int width, int height, bool clear = true) 
        : this(Raylib.LoadRenderTexture(width, height))
    {
        if (clear) Clear();
    }

    public RenderTexture(int width, int height, Color4 clearColor, bool clear = true) 
        : this(Raylib.LoadRenderTexture(width, height))
    {
        ClearColor = clearColor;
        if (clear) Clear();
    }

    public void Clear()
    {
        Raylib.BeginTextureMode(Raw);
        Raylib.ClearBackground(ClearColor);
        Raylib.EndTextureMode();
    }

    public void CleanResize(int width, int height)
    {
        Raylib.UnloadRenderTexture(Raw);

        Raw = Raylib.LoadRenderTexture(width, height);

        Clear();
    }

    public void Resize(int width, int height)
    {
        var nrt = Raylib.LoadRenderTexture(width, height);

        Raylib.BeginTextureMode(nrt);
        Raylib.ClearBackground(ClearColor);
        Raylib.DrawTexture(Raw.Texture, 0, 0, Color.White);
        Raylib.EndTextureMode();

        Raylib.UnloadRenderTexture(Raw);
        Raw = nrt;
    }

    public static implicit operator RenderTexture2D(RenderTexture t) => t.Raw;

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        Unloader.Enqueue(Raw);
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~RenderTexture()
    {
        Dispose();
    }
}