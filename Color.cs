namespace Tiler.Editor;

using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;
using System;

public struct Color4(byte r, byte g, byte b, byte a = 255) : IEquatable<Color4>
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public Color4() : this(0, 0, 0, 255) {}
    public Color4(Raylib_cs.Color color) : this(color.R, color.G, color.B, color.A) {}

    public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    } 

    public static implicit operator Color(Color4 c) => new(c.R, c.G, c.B, c.A);
    public static implicit operator Color3(Color4 c) => new(c.R, c.G, c.B);
    public static implicit operator Vector3(Color4 c) => new(c.R/255f, c.G/255f, c.B/255f);
    public static implicit operator Vector4(Color4 c) => new(c.R/255f, c.G/255f, c.B/255f, c.A/255f);
    public static implicit operator ImColor(Color4 c) => new() { Value = new Vector4(c.R/255f, c.G/255f, c.B/255f, c.A/255f) };

    public static implicit operator Color4(Color c) => new(c.R, c.G, c.B, c.A);
    public static implicit operator Color4(Color3 c) => new(c.R, c.G, c.B, 255);

    public static bool operator==(Color4 lhs, Color4 rhs) => lhs.GetHashCode() == rhs.GetHashCode();
    public static bool operator!=(Color4 lhs, Color4 rhs) => lhs.GetHashCode() != rhs.GetHashCode();

    public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);
    public readonly override bool Equals([NotNullWhen(true)] object? obj) => (obj is Color4 c) && GetHashCode() == c.GetHashCode();
    public readonly override string ToString() => $"Color({R}, {G}, {B}, {A})";

    public bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;
}

public struct Color3(byte r, byte g, byte b) : IEquatable<Color3>
{
    public byte R = r;
    public byte G = g;
    public byte B = b;

    public Color3() : this(0, 0, 0) {}
    public Color3(Raylib_cs.Color color) : this(color.R, color.G, color.B) {}

    public readonly void Deconstruct(out byte r, out byte g, out byte b)
    {
        r = R;
        g = G;
        b = B;
    } 

    public static implicit operator Color(Color3 c) => new(c.R, c.G, c.B, (byte)255);
    public static implicit operator Color4(Color3 c) => new(c.R, c.G, c.B, 255);
    public static implicit operator Vector3(Color3 c) => new(c.R/255f, c.G/255f, c.B/255f);
    public static implicit operator Vector4(Color3 c) => new(c.R/255f, c.G/255f, c.B/255f, 1.0f);
    public static implicit operator ImColor(Color3 c) => new() { Value = new Vector4(c.R/255f, c.G/255f, c.B/255f, 1.0f) };

    public static implicit operator Color3(Color4 c) => new(c.R, c.G, c.B);
    public static implicit operator Color3(Color c) => new(c.R, c.G, c.B);

    public static bool operator==(Color3 lhs, Color3 rhs) => lhs.GetHashCode() == rhs.GetHashCode();
    public static bool operator!=(Color3 lhs, Color3 rhs) => lhs.GetHashCode() != rhs.GetHashCode();

    public readonly override int GetHashCode() => HashCode.Combine(R, G, B);
    public readonly override bool Equals([NotNullWhen(true)] object? obj) => (obj is Color3 c) && GetHashCode() == c.GetHashCode();
    public readonly override string ToString() => $"Color({R}, {G}, {B})";

    public bool Equals(Color3 other) => R == other.R && G == other.G && B == other.B;
}