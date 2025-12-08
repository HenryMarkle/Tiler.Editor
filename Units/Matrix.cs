namespace Tiler.Editor.Units;

public record struct MatrixUnit(int Value)
{
    public const int Ratio = 20;

    public static MatrixUnit operator+(MatrixUnit lhs, MatrixUnit rhs) => new(lhs.Value + rhs.Value);
    public static MatrixUnit operator-(MatrixUnit lhs, MatrixUnit rhs) => new(lhs.Value - rhs.Value);
    public static MatrixUnit operator*(MatrixUnit lhs, int rhs) => new(lhs.Value * rhs);
    public static MatrixUnit operator/(MatrixUnit lhs, int rhs) => new(lhs.Value / rhs);
    public static MatrixUnit operator*(MatrixUnit lhs, float rhs) => new((int) (lhs.Value * rhs));
    public static MatrixUnit operator/(MatrixUnit lhs, float rhs) => new((int) (lhs.Value / rhs));

    public readonly int Int => Value * Ratio;

    public static implicit operator int(MatrixUnit mu) => mu.Int;
}