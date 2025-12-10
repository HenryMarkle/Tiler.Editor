namespace Tiler.Editor;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

public class Quad
{
    public Vector2 TopLeft;
    public Vector2 TopRight;
    public Vector2 BottomRight;
    public Vector2 BottomLeft;

    public Quad()
    {
        TopLeft = Vector2.Zero;
        TopRight = Vector2.Zero;
        BottomRight = Vector2.Zero;
        BottomLeft = Vector2.Zero;
    }

    public Quad(
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    )
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public Quad(in Rectangle rectangle)
    {
        TopLeft = rectangle.Position;
        TopRight = new Vector2(rectangle.X + rectangle.Width, rectangle.Y);
        BottomRight = new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);
        BottomLeft = new Vector2(rectangle.X, rectangle.Y + rectangle.Height);
    }

    public static Quad operator+(Quad lhs, Quad rhs) => new(
        lhs.TopLeft + rhs.TopLeft, 
        lhs.TopRight + rhs.TopRight, 
        lhs.BottomRight + rhs.BottomRight, 
        lhs.BottomLeft + rhs.BottomLeft
    );

    public static Quad operator-(Quad lhs, Quad rhs) => new(
        lhs.TopLeft - rhs.TopLeft, 
        lhs.TopRight - rhs.TopRight, 
        lhs.BottomRight - rhs.BottomRight, 
        lhs.BottomLeft - rhs.BottomLeft
    );

    public static Quad operator+(Quad lhs, Vector2 rhs) => new(
        lhs.TopLeft + rhs, 
        lhs.TopRight + rhs, 
        lhs.BottomRight + rhs, 
        lhs.BottomLeft + rhs
    );

    public static Quad operator-(Quad lhs, Vector2 rhs) => new(
        lhs.TopLeft - rhs, 
        lhs.TopRight - rhs, 
        lhs.BottomRight - rhs, 
        lhs.BottomLeft - rhs
    );

    public Vector2 Center => (TopLeft + TopRight + BottomRight + BottomLeft) / 4;

    public Quad Rotate(int degrees, Vector2 center)
    {
        var radian = float.DegreesToRadians(degrees);
        
        var sinRotation = (float)Math.Sin(radian);
        var cosRotation = (float)Math.Cos(radian);
        
        Vector2 newTopLeft, newTopRight, newBottomRight, newBottomLeft;

        { // Rotate the top left corner
            var x = TopLeft.X;
            var y = TopLeft.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            newTopLeft = new Vector2(
                center.X + dx * cosRotation - dy * sinRotation, 
                center.Y + dx * sinRotation + dy * cosRotation
            );
        }
        
        { // Rotate the top right corner
            var x = TopRight.X;
            var y = TopRight.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            newTopRight = new Vector2(center.X + dx * cosRotation - dy * sinRotation, center.Y + dx * sinRotation + dy * cosRotation);
        }
        
        { // Rotate the bottom right corner
            var x = BottomRight.X;
            var y = BottomRight.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            newBottomRight = new Vector2(center.X + dx * cosRotation - dy * sinRotation, center.Y + dx * sinRotation + dy * cosRotation);
        }
        
        { // Rotate the bottom left corner
            var x = BottomLeft.X;
            var y = BottomLeft.Y;

            var dx = x - center.X;
            var dy = y - center.Y;

            newBottomLeft = new Vector2(center.X + dx * cosRotation - dy * sinRotation, center.Y + dx * sinRotation + dy * cosRotation);
        }

        return new Quad(newTopLeft, newTopRight, newBottomRight, newBottomLeft);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quad Rotate(int degrees) => Rotate(degrees, Center);

    public override string ToString() => $"Quad({TopLeft}, {TopRight}, {BottomRight}, {BottomLeft})";
}