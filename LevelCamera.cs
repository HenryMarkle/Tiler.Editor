using System;
using System.Numerics;

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

    public readonly Vector2 Position => new(
        Distance * MathF.Cos(float.DegreesToRadians(Angle)),
        Distance * MathF.Sin(float.DegreesToRadians(Angle))
    );
}

public class LevelCamera
{
    public Vector2 Position;

    public LevelCameraVertex TopLeft;
    public LevelCameraVertex TopRight;
    public LevelCameraVertex BottomRight;
    public LevelCameraVertex BottomLeft;

    public LevelCamera()
    {
        Position = Vector2.Zero;
        
        TopLeft = new();
        TopRight = new();
        BottomRight = new();
        BottomLeft = new();
    }

    public LevelCamera(Vector2 position)
    {
        Position = position;
        
        TopLeft = new();
        TopRight = new();
        BottomRight = new();
        BottomLeft = new();
    }
}