using System;
using System.Numerics;
using Raylib_cs;

namespace Tiler.Editor;

public struct LevelCameraVertex
{
    public float Distance;
    public int Angle;

    public LevelCameraVertex()
    {
        Distance = 0;
        Angle = 0;
    }
    
    public LevelCameraVertex(float distance, int angle)
    {
        Distance = distance;
        Angle = angle;
    }

    public Vector2 Position
    {
        readonly get => new(
            Distance * MathF.Cos(float.DegreesToRadians(Angle)),
            Distance * MathF.Sin(float.DegreesToRadians(Angle))
        );

        set
        {
            Distance = Raymath.Vector2Length(value);
            Angle = (int)float.RadiansToDegrees(MathF.Atan2(value.Y, value.X));
        }
    }
}

public class LevelCamera
{
    public const int Width = 1400;
    public const int Height = 800;
    
    public Vector2 Position;

    public LevelCameraVertex TopLeft;
    public LevelCameraVertex TopRight;
    public LevelCameraVertex BottomRight;
    public LevelCameraVertex BottomLeft;

    public LevelCamera()
    {
        Position = Vector2.Zero;
        
        TopLeft = new(50, 45);
        TopRight = new(50, 90 + 45);
        BottomRight = new(50, 180 + 45);
        BottomLeft = new(50, -45);
    }

    public LevelCamera(Vector2 position)
    {
        Position = position;
        
        TopLeft = new(50, 45);
        TopRight = new(50, 90 + 45);
        BottomRight = new(50, 180 + 45);
        BottomLeft = new(50, -45);
    }
}