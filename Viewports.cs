namespace Tiler.Editor;

using System.Drawing;
using Tiler.Editor.Managed;

public class Viewports
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }

    public static int LightmapMargin { get; set; } = 100;

    public RenderTexture Main { get; private set; }
    public RenderTexture Lightmap { get; private set; }
    public RenderTexture[] Geos { get; private set; }
    public RenderTexture[] Tiles { get; private set; }

    void Resize(int width, int height)
    {
        Main.Resize(width, height);
        Main.Resize(width + LightmapMargin*2, height + LightmapMargin*2);

        for (int l = 0; l < Depth; l++)
        {
            Geos[l].Resize(width, height);
            Tiles[l].Resize(width, height);
        }

        Width = width;
        Height = height;
    }

    public Viewports(int width, int height, int depth)
    {
        Width = width;
        Height = height;
        Depth = depth;

        Main = new(width, height, new Color4(0,0,0,0), true);
        Lightmap = new(width + LightmapMargin*2, height + LightmapMargin*2, new Color4(0,0,0,0), true);

        Geos = new RenderTexture[depth];
        Tiles = new RenderTexture[depth];

        for (int l = 0; l < depth; l++)
        {
            Geos[l] = new(width, height, new Color4(0,0,0,0), true);
            Tiles[l] = new(width, height, new Color4(0,0,0,0), true);
        }
    }
}