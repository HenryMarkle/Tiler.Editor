using System.Runtime.InteropServices;

namespace Tiler.Editor;

using System;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using Raylib_cs;
using ImGuiNET;

public readonly struct Color4(byte r, byte g, byte b, byte a = 255) : IEquatable<Color4>
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;

    public Color4() : this(r: 0, g: 0, b: 0, a: 0) {}
    public Color4(Raylib_cs.Color color) : this(color.R, color.G, color.B, color.A) {}

    public static Color4 White => new(r: 255, g: 255, b: 255, a: 255);
    public static Color4 Black => new(r: 0, g: 0, b: 0, a: 255);
    public static Color4 Red => new(r: 255, g: 0, b: 0, a: 255);
    public static Color4 Green => new(r: 0, g: 255, b: 0, a: 255);
    public static Color4 Blue => new(r: 0, g: 0, b: 255, a: 255);
    public static Color4 Purple => new(r: 255, g: 0, b: 255, a: 255);
    
    private const float Inv255 = 1.0f / 255.0f;

    public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    } 

    public static implicit operator Color(Color4 c) => new(c.R, c.G, c.B, c.A);
    public static implicit operator Color3(Color4 c) => new(c.R, c.G, c.B);
    public static implicit operator Vector3(Color4 c) => new(c.R*Inv255, c.G*Inv255, c.B*Inv255);
    public static implicit operator Vector4(Color4 c) => new(c.R*Inv255, c.G*Inv255, c.B*Inv255, c.A*Inv255);
    public static implicit operator ImColor(Color4 c) => new() { Value = new Vector4(c.R*Inv255, c.G*Inv255, c.B*Inv255, c.A*Inv255) };

    public static implicit operator Color4(Color c) => new(c.R, c.G, c.B, c.A);
    public static implicit operator Color4(Color3 c) => new(c.R, c.G, c.B, a: 255);

    public static bool operator==(Color4 lhs, Color4 rhs) => lhs.Equals(rhs);
    public static bool operator!=(Color4 lhs, Color4 rhs) => !lhs.Equals(rhs);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public override bool Equals([NotNullWhen(true)] object? obj) => 
        obj is Color4 c && Equals(c);
    public override string ToString() => $"Color({R}, {G}, {B}, {A})";

    public bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;
}

public readonly struct Color3(byte r, byte g, byte b) : IEquatable<Color3>
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;

    public Color3() : this(r: 0, g: 0, b: 0) {}
    public Color3(Raylib_cs.Color color) : this(color.R, color.G, color.B) {}
    
    public static Color3 White => new(r: 255, g: 255, b: 255);
    public static Color3 Black => new(r: 0, g: 0, b: 0);
    public static Color3 Red => new(r: 255, g: 0, b: 0);
    public static Color3 Green => new(r: 0, g: 255, b: 0);
    public static Color3 Blue => new(r: 0, g: 0, b: 255);
    public static Color3 Purple => new(r: 255, g: 0, b: 255);
    
    private const float Inv255 = 1.0f / 255.0f;

    public void Deconstruct(out byte r, out byte g, out byte b)
    {
        r = R;
        g = G;
        b = B;
    } 

    public static implicit operator Color(Color3 c) => new(c.R, c.G, c.B, (byte)255);
    public static implicit operator Color4(Color3 c) => new(c.R, c.G, c.B, 255);
    public static implicit operator Vector3(Color3 c) => new(c.R*Inv255, c.G*Inv255, c.B*Inv255);
    public static implicit operator Vector4(Color3 c) => new(c.R*Inv255, c.G*Inv255, c.B*Inv255, 1.0f);
    public static implicit operator ImColor(Color3 c) => new() { Value = new Vector4(c.R*Inv255, c.G*Inv255, c.B*Inv255, 1.0f) };

    public static implicit operator Color3(Color4 c) => new(c.R, c.G, c.B);
    public static implicit operator Color3(Color c) => new(c.R, c.G, c.B);

    public static bool operator==(Color3 lhs, Color3 rhs) => lhs.Equals(rhs);
    public static bool operator!=(Color3 lhs, Color3 rhs) => !lhs.Equals(rhs);

    public override int GetHashCode() => HashCode.Combine(R, G, B);
    public override bool Equals([NotNullWhen(true)] object? obj) => 
        obj is Color3 c && Equals(c);
    public override string ToString() => $"Color({R}, {G}, {B})";

    public bool Equals(Color3 other) => R == other.R && G == other.G && B == other.B;
}