namespace Tiler.Editor;

using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;
using System;

public struct Color4(byte r, byte g, byte b, byte a = 255)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public Color4() : this(0, 0, 0, 255) {}

    public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    } 

    public static implicit operator Color(Color4 c) => new(c.R, c.G, c.B, c.A);
    public static implicit operator Vector3(Color4 c) => new(c.R/255f, c.G/255f, c.B/255f);
    public static implicit operator Vector4(Color4 c) => new(c.R/255f, c.G/255f, c.B/255f, c.A/255f);
    public static implicit operator ImColor(Color4 c) => new() { Value = new Vector4(c.R/255f, c.G/255f, c.B/255f, c.A/255f) };

    public static implicit operator Color4(Color c) => new(c.R, c.G, c.B, c.A);

    public static bool operator==(Color4 lhs, Color4 rhs) => lhs.GetHashCode() == rhs.GetHashCode();
    public static bool operator!=(Color4 lhs, Color4 rhs) => lhs.GetHashCode() != rhs.GetHashCode();

    public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Color4 c) && GetHashCode() == c.GetHashCode();
    public override readonly string ToString() => $"Color({R}, {G}, {B}, {A})";
}