using System.Collections.Generic;
using Raylib_cs;
using Serilog;

namespace Tiler.Editor.Managed;

public static class Unloader
{
    private static readonly Queue<Texture2D> textures = [];
    private static readonly Queue<RenderTexture2D> renderTextures = [];

    public static void Enqueue(Texture2D texture) => textures.Enqueue(texture);
    public static void Enqueue(RenderTexture2D texture) => renderTextures.Enqueue(texture);
    public static void Dequeue(int cap)
    {
        int capacity = 0;

        while (capacity++ < cap && textures.Count != 0)
        {
            Raylib.UnloadTexture(textures.Dequeue());
        }
        
        while (capacity++ < cap && renderTextures.Count != 0) 
        {
            Raylib.UnloadRenderTexture(renderTextures.Dequeue());
        }
    }
}