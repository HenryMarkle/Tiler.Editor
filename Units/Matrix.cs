namespace Tiler.Editor.Units;

public readonly record struct MatrixUnit(int Value) : IUnit<MatrixUnit, int>
{
   public static MatrixUnit operator +(MatrixUnit lhs, MatrixUnit rhs) => new(lhs.Value + rhs.Value);
   public static MatrixUnit operator -(MatrixUnit lhs, MatrixUnit rhs) => new(lhs.Value - rhs.Value);
   
   public static float operator /(MatrixUnit lhs, MatrixUnit rhs) => (float) lhs.Value / rhs.Value;
   
   public static MatrixUnit operator *(MatrixUnit lhs, int rhs) => new(lhs.Value * rhs);
   public static MatrixUnit operator /(MatrixUnit lhs, int rhs) => new(lhs.Value / rhs);
   public static MatrixUnit operator *(MatrixUnit lhs, float rhs) => new((int)(lhs.Value * rhs));
   public static MatrixUnit operator /(MatrixUnit lhs, float rhs) => new((int)(lhs.Value / rhs));
   
   public static implicit operator PixelUnit(MatrixUnit pixel) => new(pixel.Value * 20);
}