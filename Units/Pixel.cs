namespace Tiler.Editor.Units;

public readonly record struct PixelUnit(int Value) : IUnit<PixelUnit, int>
{
    public static PixelUnit operator +(PixelUnit lhs, PixelUnit rhs) => new(lhs.Value + rhs.Value);
    public static PixelUnit operator -(PixelUnit lhs, PixelUnit rhs) => new(lhs.Value - rhs.Value);
    
    public static float operator /(PixelUnit lhs, PixelUnit rhs) => (float) lhs.Value / rhs.Value;
    
    public static PixelUnit operator *(PixelUnit lhs, int rhs) => new(lhs.Value * rhs);
    public static PixelUnit operator /(PixelUnit lhs, int rhs) => new(lhs.Value / rhs);
    
    public static PixelUnit operator *(PixelUnit lhs, float rhs) => new((int)(lhs.Value * rhs));
    public static PixelUnit operator /(PixelUnit lhs, float rhs) => new((int)(lhs.Value / rhs));
    
    public static implicit operator MatrixUnit(PixelUnit pixel) => new(pixel.Value / 20);
}