using System.Runtime.CompilerServices;
using Raylib_cs;
using Tiler.Editor.Managed;

namespace Tiler.Editor;

public class GeoAtlas(Texture texture)
{
    public Texture Texture { get; init; } = texture;

    public static implicit operator Texture2D(GeoAtlas atlas) => atlas.Texture.Raw;

    public (int x, int y) Air = (0, 0);
    public (int x, int y) Solid = (1, 0);
    public (int x, int y) HorizontalPole = (2, 0);
    public (int x, int y) CrossPole = (3, 0);
    public (int x, int y) Slab = (4, 0);
    public (int x, int y) Glass = (0, 1);
    public (int x, int y) Exit = (1, 1);
    public (int x, int y) Wall = (2, 1);
    public (int x, int y) VerticalPole = (3, 1);
    public (int x, int y) Platform = (4, 1);

    public (int x, int y) SlopeNW = (5, 0);
    public (int x, int y) SlopeNE = (6, 0);
    public (int x, int y) SlopeSW = (5, 1);
    public (int x, int y) SlopeSE = (6, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int x, int y) GetIndex(Geo g) => g switch
    {
        Geo.Air => Air,
        Geo.Solid => Solid,
        Geo.HorizontalPole => HorizontalPole,
        Geo.CrossPole => CrossPole,
        Geo.Slab => Slab,
        Geo.Glass => Glass,
        Geo.Exit => Exit,
        Geo.Wall => Wall,
        Geo.VerticalPole => VerticalPole,
        Geo.Platform => Platform,
        Geo.SlopeNW => SlopeNW,
        Geo.SlopeNE => SlopeNE,
        Geo.SlopeSW => SlopeSW,
        Geo.SlopeSE => SlopeSE,
        
        _ => Air
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle GetRect((int x, int y) index) => new(index.x * 20f, index.y *20f, 20f, 20f);
}