using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Tiler.Editor;

/// <summary>
/// A 3-dimensional array.
/// </summary>
public class Matrix<T>(int width, int height, int depth)
{
    private T[,,] array = new T[width, height, depth];

    public T this[int x, int y, int z]
    {
        get => array[x, y, z];
        set => array[x, y, z] = value;
    }
    
    public T At(int x, int y = 0, int z = 0) => array[x, y, z];
    public string? StrAt(int x, int y, int z) => array[x, y, z]?.ToString();
    public T AtFlatIndex(int index) => array[
        index % Width,
        (index % (Width * Height)) / Width,
        index / (Width * Height)
    ];

    public int Width => array.GetLength(dimension: 0);
    public int Height => array.GetLength(dimension: 1);
    public int Depth => array.GetLength(dimension: 2);

    public void Resize(int nWidth, int nHeight)
    {
        var nArray = new T[nWidth, nHeight, Depth];

        for (var z = 0; z < Depth; z++)
        {
            for (var y = 0; y < Math.Max(Height, nHeight); y++)
            {
                for (var x = 0; x < Math.Max(Width, nWidth); x++)
                {
                    nArray[x, y, z] = array[x, y, z];
                }
            }
        }

        array = nArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(int x, int y, int z) => 
        x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(Vector2 pos) => 
        pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

    public override string ToString() => $"Matrix({Width}, {Height}, {Depth})";
}