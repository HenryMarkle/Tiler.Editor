using System.Runtime.CompilerServices;

namespace Tiler.Editor;

/// <summary>
/// Represents a collide-able, geometric shape in the level.
/// </summary>
public enum Geo : byte
{
    Air            = 0, 
    Solid          = 1, 
    Wall           = 2,
    SlopeNW        = 3, 
    SlopeNE        = 4, 
    SlopeSE        = 5, 
    SlopeSW        = 6,
    Platform       = 8,
    Slab           = 9,
    Glass          = 10,
    Exit           = 11,
    VerticalPole   = 12,
    HorizontalPole = 13,
    CrossPole      = 14,
}

public static class GeoExtensions
{
    extension(Geo geo)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSolid() => geo is Geo.Solid or Geo.Wall or Geo.Glass;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSlope() => geo is Geo.SlopeNW or Geo.SlopeNE or Geo.SlopeSE or Geo.SlopeSW;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPole() => geo is Geo.VerticalPole or Geo.HorizontalPole or Geo.CrossPole;
    }
}