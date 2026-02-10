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