using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Tiler.Editor;

public class Matrix<T>
{
    private T[,,] array;

    public T this[int x, int y, int z]
    {
        get => array[x, y, z];
        set => array[x, y, z] = value;
    }

    public T At(int x, int y = 0, int z = 0) => array[x, y, z];
    public string? StrAt(int x, int y, int z) => array[x, y, z]?.ToString();

    public int Width => array.GetLength(0);
    public int Height => array.GetLength(1);
    public int Depth => array.GetLength(2);

    public Matrix(int width, int height, int depth)
    {
        array = new T[width, height, depth];
    }

    public void Resize(int nwidth, int nheight)
    {
        var narray = new T[nwidth, nheight, Depth];

        for (int z = 0; z < Depth; z++)
        {
            for (int y = 0; y < Math.Max(Height, nheight); y++)
            {
                for (int x = 0; x < Math.Max(Width, nwidth); x++)
                {
                    narray[x, y, z] = array[x, y, z];
                }
            }
        }

        array = narray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(int x, int y, int z) => 
        x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(Vector2 pos) => 
        pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

    public override string ToString() => $"Matrix({Width}, {Height}, {Depth})";
}