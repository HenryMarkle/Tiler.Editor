namespace Tiler.Editor;

public enum Geo : byte
{
    Air, 
    Solid, 
    Wall,
    SlopeNW, 
    SlopeNE, 
    SlopeSE, 
    SlopeSW,
    Platform,
    Slab,
    Glass,
    Exit,
    VerticalPole,
    HorizontalPole,
    CrossPole,
}