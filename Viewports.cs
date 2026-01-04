namespace Tiler.Editor;

using System.Drawing;
using Tiler.Editor.Managed;

public class Viewports
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }

    public static int LightmapMargin { get; set; } = 5 * 10;

    public RenderTexture Main { get; private set; }
    public RenderTexture Connections { get; private set; }
    public RenderTexture Lightmap { get; private set; }
    public RenderTexture Effect { get; private set; }
    public RenderTexture Props { get; private set; }
    public RenderTexture[] Geos { get; private set; }
    public RenderTexture[] Tiles { get; private set; }

    public void Resize(int width, int height)
    {
        Main.Resize(width, height);
        Connections.Resize(width, height);
        Lightmap.Resize(width + LightmapMargin*2, height + LightmapMargin*2);
        Effect.Resize(width, height);
        Props.Resize(width, height);

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
        Connections = new(width, height, new Color4(0,0,0,0), true);
        Lightmap = new(width + LightmapMargin*2, height + LightmapMargin*2, new Color4(0,0,0,0), true);
        Effect = new(width, height, new Color4(0,0,0,0), true);
        Props = new(width, height, new Color4(0,0,0,0), true);

        Geos = new RenderTexture[depth];
        Tiles = new RenderTexture[depth];

        for (int l = 0; l < depth; l++)
        {
            Geos[l] = new(width, height, new Color4(0,0,0,0), true);
            Tiles[l] = new(width, height, new Color4(0,0,0,0), true);
        }
    }
}