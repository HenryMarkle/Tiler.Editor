using System;
using System.Numerics;

namespace Tiler.Editor;

public struct Triangle(Vector2 a, Vector2 b, Vector2 c) : IEquatable<Triangle>
{
    public Vector2 A = a;
    public Vector2 B = b;
    public Vector2 C = c;
    
    public Triangle(Triangle triangle) : this(triangle.A, triangle.B, triangle.C) { }

    public void Deconstruct(out Vector2 a, out Vector2 b, out Vector2 c)
    {
        a = A;
        b = B;
        c = C;
    }
    
    public override string ToString() => $"Triangle({A}, {B}, {C})";

    public bool Equals(Triangle other)
    {
        return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);
    }

    public override bool Equals(object? obj)
    {
        return obj is Triangle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B, C);
    }
}