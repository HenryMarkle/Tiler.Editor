using System.Collections.Generic;
using Raylib_cs;
using Serilog;

namespace Tiler.Editor.Managed;

public static class Unloader
{
    private static readonly Queue<Texture2D> textures = [];
    private static readonly Queue<RenderTexture2D> renderTextures = [];
    private static readonly Queue<Raylib_cs.Shader> shaders = [];

    public static void Enqueue(Texture2D texture) => textures.Enqueue(texture);
    public static void Enqueue(RenderTexture2D texture) => renderTextures.Enqueue(texture);
    public static void Enqueue(Raylib_cs.Shader shader) => shaders.Enqueue(shader);
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
        
        while (capacity++ < cap && shaders.Count != 0) 
        {
            Raylib.UnloadShader(shaders.Dequeue());
        }
    }
}