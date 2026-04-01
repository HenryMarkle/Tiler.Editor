namespace Tiler.Editor;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Raylib_cs;

public class Quad(
    Vector2 topLeft,
    Vector2 topRight,
    Vector2 bottomRight,
    Vector2 bottomLeft
    )
{
    public Vector2 TopLeft = topLeft;
    public Vector2 TopRight = topRight;
    public Vector2 BottomRight = bottomRight;
    public Vector2 BottomLeft = bottomLeft;

    public Quad() : this(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero)
    {
    }

    public Quad(Quad quad) : this(quad.TopLeft, quad.TopRight, quad.BottomRight, quad.BottomLeft)
    {
    }

    public Quad(in Rectangle rectangle) : this(
        topLeft: rectangle.Position, 
        topRight: new Vector2(rectangle.X + rectangle.Width, rectangle.Y), 
        bottomRight: new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height), 
        bottomLeft: new Vector2(rectangle.X, rectangle.Y + rectangle.Height)
        )
    {
    }

    public static Quad operator+(Quad lhs, Quad rhs) => new(
        topLeft:     lhs.TopLeft + rhs.TopLeft, 
        topRight:    lhs.TopRight + rhs.TopRight, 
        bottomRight: lhs.BottomRight + rhs.BottomRight, 
        bottomLeft:  lhs.BottomLeft + rhs.BottomLeft
    );

    public static Quad operator-(Quad lhs, Quad rhs) => new(
        topLeft:     lhs.TopLeft - rhs.TopLeft, 
        topRight:    lhs.TopRight - rhs.TopRight, 
        bottomRight: lhs.BottomRight - rhs.BottomRight, 
        bottomLeft:  lhs.BottomLeft - rhs.BottomLeft
    );

    public static Quad operator+(Quad lhs, Vector2 rhs) => new(
        topLeft:     lhs.TopLeft + rhs, 
        topRight:    lhs.TopRight + rhs, 
        bottomRight: lhs.BottomRight + rhs, 
        bottomLeft:  lhs.BottomLeft + rhs
    );

    public static Quad operator-(Quad lhs, Vector2 rhs) => new(
        topLeft:     lhs.TopLeft - rhs, 
        topRight:    lhs.TopRight - rhs, 
        bottomRight: lhs.BottomRight - rhs, 
        bottomLeft:  lhs.BottomLeft - rhs
    );

    public static Quad operator *(Quad lhs, int rhs) => new(
        topLeft:     lhs.TopLeft * rhs, 
        topRight:    lhs.TopRight * rhs, 
        bottomRight: lhs.BottomRight * rhs, 
        bottomLeft:  lhs.BottomLeft * rhs
        );
    
    public static Quad operator /(Quad lhs, int rhs) => new(
        topLeft:     lhs.TopLeft / rhs, 
        topRight:    lhs.TopRight / rhs, 
        bottomRight: lhs.BottomRight / rhs, 
        bottomLeft:  lhs.BottomLeft / rhs
    );

    public static Quad operator *(Quad lhs, float rhs) => new(
        topLeft:     lhs.TopLeft * rhs, 
        topRight:    lhs.TopRight * rhs, 
        bottomRight: lhs.BottomRight * rhs, 
        bottomLeft:  lhs.BottomLeft * rhs
        );
    
    public static Quad operator /(Quad lhs, float rhs) => new(
        topLeft:     lhs.TopLeft / rhs, 
        topRight:    lhs.TopRight / rhs, 
        bottomRight: lhs.BottomRight / rhs, 
        bottomLeft:  lhs.BottomLeft / rhs
    );
    
    public Vector2 Center => (TopLeft + TopRight + BottomRight + BottomLeft) / 4;

    public void Deconstruct(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomRight, out Vector2 bottomLeft)
    {
        topLeft     = TopLeft;
        topRight    = TopRight;
        bottomRight = BottomRight;
        bottomLeft  = BottomLeft;
    }

    public (Vector2 left, Vector2 right) HorizontalPoints => (
        (TopLeft + BottomLeft) / 2,
        (TopRight + BottomRight) / 2
        );

    public (Vector2 top, Vector2 bottom) VerticalPoints => (
        (TopLeft + TopRight) / 2,
        (BottomLeft + BottomRight) / 2
        );

    public void Rotate(int degrees, Vector2 center)
    {
        var radian = float.DegreesToRadians(degrees);
        
        var sinRotation = (float)Math.Sin(radian);
        var cosRotation = (float)Math.Cos(radian);
        
        { // Rotate the top left corner
            var x = TopLeft.X;
            var y = TopLeft.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            TopLeft = new Vector2(
                center.X + dx * cosRotation - dy * sinRotation, 
                center.Y + dx * sinRotation + dy * cosRotation
            );
        }
        
        { // Rotate the top right corner
            var x = TopRight.X;
            var y = TopRight.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            TopRight = new Vector2(
                center.X + dx * cosRotation - dy * sinRotation, 
                center.Y + dx * sinRotation + dy * cosRotation);
        }
        
        { // Rotate the bottom right corner
            var x = BottomRight.X;
            var y = BottomRight.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            BottomRight = new Vector2(
                center.X + dx * cosRotation - dy * sinRotation, 
                center.Y + dx * sinRotation + dy * cosRotation);
        }
        
        { // Rotate the bottom left corner
            var x = BottomLeft.X;
            var y = BottomLeft.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            BottomLeft = new Vector2(
                center.X + dx * cosRotation - dy * sinRotation, 
                center.Y + dx * sinRotation + dy * cosRotation);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Rotate(int degrees) => Rotate(degrees, Center);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rectangle Enclosed()
    {
        var minX = MathF.Min(MathF.Min(TopLeft.X, TopRight.X), MathF.Min(BottomRight.X, BottomLeft.X));
        var minY = MathF.Min(MathF.Min(TopLeft.Y, TopRight.Y), MathF.Min(BottomRight.Y, BottomLeft.Y));
        
        var maxX = MathF.Max(MathF.Max(TopLeft.X, TopRight.X), MathF.Max(BottomRight.X, BottomLeft.X));
        var maxY = MathF.Max(MathF.Max(TopLeft.Y, TopRight.Y), MathF.Max(BottomRight.Y, BottomLeft.Y));

        return new Rectangle(x: minX, y: minY, width: maxX - minX, height: maxY - minY);
    }

    public override string ToString() => $"Quad({TopLeft}, {TopRight}, {BottomRight}, {BottomLeft})";
}