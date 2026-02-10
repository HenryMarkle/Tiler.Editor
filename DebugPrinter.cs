namespace Tiler.Editor;

using Raylib_cs;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public class DebugPrinter
{
    public Color4 TextColor = new(255, 255, 255);
    public Color4 BackgroundColor = new(100, 100, 100, 100);

    public required Font Font;
    public required int Size;
    public Vector2 Margin = new(3, 4);
    public Vector2 Anchor = Vector2.Zero;
    public Vector2 Cursor = Vector2.Zero;

    public void Reset()
    {
        Cursor = Anchor;
    } 

    public void NewLine(int count = 1)
    {
        Cursor.X = 0;
        Cursor.Y += count * (Size + Margin.Y * 2);
    }

    public void Print<T>(T e, Color4 color)
    {
        var text = e?.ToString() ?? "NULL";
        var measured = Raylib.MeasureTextEx(Font, text, Size, 0.1f);

        Raylib.DrawRectangleRec(
            new Rectangle(Cursor.X, Cursor.Y, measured.X + Margin.X * 2, measured.Y + Margin.Y * 2),
            BackgroundColor
        );

        Raylib.DrawTextEx(Font, text, Cursor + Margin, Size, 0.1f, color);

        Cursor.X += measured.X + Margin.X*2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Print<T>(T e)
    {
        Print(e, TextColor);
    }

    public void Println<T>(T e, Color4 color)
    {
        var text = e?.ToString() ?? "NULL";
        var measured = Raylib.MeasureTextEx(Font, text, Size, 0.1f);

        Raylib.DrawRectangleRec(
            new Rectangle(Cursor.X, Cursor.Y, measured.X + Margin.X * 2, measured.Y + Margin.Y * 2),
            BackgroundColor
        );

        Raylib.DrawTextEx(Font, text, Cursor + Margin, Size, 0.1f, color);
        
        NewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Println<T>(T e)
    {
        Println(e, TextColor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrintLabel<T>(string label, T e, Color4 color)
    {
        Print(label);
        Print(e, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrintlnLabel<T>(string label, T e, Color4 color)
    {
        Print(label);
        Println(e, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrintLabel<T>(string label, T e)
    {
        Print(label);
        Print(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrintlnLabel<T>(string label, T e)
    {
        Print(label);
        Println(e);
    }
}