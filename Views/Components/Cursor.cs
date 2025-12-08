using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Raylib_cs;
using static Raylib_cs.Raylib;

using ImGuiNET;

namespace Tiler.Editor.Views.Components;

public class Cursor(Context context)
{
    private readonly Context Context = context;

    public float X { get; private set; }
    public float Y { get; private set; }

    public int MX { get; private set; }
    public int MY { get; private set; }

    public bool IsInMatrix { get; private set; }
    public bool IsInWindow { get; private set; }
    public bool IsSelecting { get; private set; }
    public bool IsErasing { get; private set; }

    private (int mx, int my) initialselectionPos = (0, 0);
    private Rectangle selection = new(0, 0, 0, 0);

    public delegate void SelectionEventHandler(Rectangle selection, bool isErasing);
    public event SelectionEventHandler? AreaSelected;

    public void SetCursor(float x, float y)
    {
        if (x == X && y == Y) return;

        X = x;
        Y = y;

        MX = (int) X / 20;
        MY = (int) Y / 20;

        if (Context.SelectedLevel is { } level)
        {
            MX = Math.Clamp(MX, 0, level.Width - 1);
            MY = Math.Clamp(MY, 0, level.Height - 1);

            IsInMatrix = X >= 0 && X < level.Width * 20 && Y >= 0 && Y < level.Height * 20;
        }
        else IsInMatrix = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCursor(Vector2 pos) => SetCursor(pos.X, pos.Y);

    public void ProcessCursor()
    {
        if (IsMouseButtonDown(MouseButton.Middle))
        {
            var delta = GetMouseDelta();
            ref var camera = ref Context.Camera;

            delta *= -1f / camera.Zoom;
            camera.Target += delta;
        }
        else
        {
            // set position

            var deltaLen = Raymath.Vector2Length(GetMouseDelta());

            if (deltaLen > 0)
            {
                var pos = GetScreenToWorld2D(GetMousePosition(), Context.Camera);
                SetCursor(pos);
            }

            // zoom

            var wheel = GetMouseWheelMove();
            ref var camera = ref Context.Camera;

            if (wheel != 0)
            {
                camera.Offset = GetMousePosition();
                camera.Target = new Vector2(X, Y);
                camera.Zoom += wheel * 0.125f;
                if (camera.Zoom < 0.125f)
                    camera.Zoom = 0.125f;
            }
        }

        float amplifier = IsKeyDown(KeyboardKey.LeftShift) * 10 + 1;

        if (IsKeyDown(KeyboardKey.LeftControl)) { // Move camera
            ref var camera = ref Context.Camera;

            if (IsKeyPressed(KeyboardKey.Left)) {
                // This is useful to keep camera position aligned with the grid
                camera.Target.X = (int)(camera.Target.X / 20) * 20;
                camera.Target.Y = (int)(camera.Target.Y / 20) * 20;
                camera.Target.X -= amplifier * 20f;
            } else if (IsKeyPressed(KeyboardKey.Right)) {
                camera.Target.X = (int)(camera.Target.X / 20) * 20;
                camera.Target.Y = (int)(camera.Target.Y / 20) * 20;
                camera.Target.X += amplifier * 20f;
            }

            if (IsKeyPressed(KeyboardKey.Up)) {
                camera.Target.X = (int)(camera.Target.X / 20) * 20;
                camera.Target.Y = (int)(camera.Target.Y / 20) * 20;
                camera.Target.Y -= amplifier * 20f;
            } else if (IsKeyPressed(KeyboardKey.Down)) {
                camera.Target.X = (int)(camera.Target.X / 20) * 20;
                camera.Target.Y = (int)(camera.Target.Y / 20) * 20;
                camera.Target.Y += amplifier * 20f;
            }
        } else { // Move cursor
            if (IsKeyPressed(KeyboardKey.Left)) {
                SetCursor(X - (20 * amplifier), Y);
            } else if (IsKeyPressed(KeyboardKey.Right)) {
                SetCursor(X + (20 * amplifier), Y);
            }

            if (IsKeyPressed(KeyboardKey.Up)) {
                SetCursor(X, Y - (20 * amplifier));
            } else if (IsKeyPressed(KeyboardKey.Down)) {
                SetCursor(X, Y + (20 * amplifier));
            }
        }
    }

    public void ProcessSelection()
    {
        if (!IsSelecting)
        {
            if (IsKeyDown(KeyboardKey.LeftShift) && (IsMouseButtonDown(MouseButton.Left) || IsMouseButtonDown(MouseButton.Right)))
            {
                IsSelecting = true;
                initialselectionPos = (MX, MY);
                selection = new Rectangle(MX, MY, 1, 1);
            }
        }
        else
        {
            if (IsKeyReleased(KeyboardKey.LeftShift) || (IsMouseButtonReleased(MouseButton.Left) || IsMouseButtonReleased(MouseButton.Right)))
            {
                IsSelecting = false;
                AreaSelected?.Invoke(selection, IsErasing);
            }
            else
            {
                var minx = Math.Min(MX, initialselectionPos.mx);
                var miny = Math.Min(MY, initialselectionPos.my);
            
                var maxx = Math.Max(MX, initialselectionPos.mx);
                var maxy = Math.Max(MY, initialselectionPos.my);
            
                selection.X = minx;
                selection.Y = miny;
                selection.Width = maxx - minx + 1;
                selection.Height = maxy - miny + 1;

                IsErasing = IsMouseButtonDown(MouseButton.Right);
            }
        }
    }

    public void DrawCursor()
    {
        if (IsSelecting)
        {
            DrawRectangleLinesEx(
                new Rectangle(
                    selection.X * 20, 
                    selection.Y * 20, 
                    selection.Width * 20, 
                    selection.Height * 20
                ),
                2,
                IsErasing ? Color.Red : Color.White
            );
        }
        else
        {
            DrawRectangleLinesEx(
                new Rectangle(
                    MX * 20, 
                    MY * 20, 
                    20, 
                    20
                ), 
                2, 
                IsErasing ? Color.Red : Color.White
            );
        }
    }

    public void DrawGrid()
    {
        if (Context.SelectedLevel is not { } level) return;

        for (int x = 0; x < level.Width; x++)
            DrawLineEx(new Vector2(x * 20, 0), new Vector2(x * 20, level.Height * 20), x % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
     
        for (int y = 0; y < level.Height; y++)
            DrawLineEx(new Vector2(0, y * 20), new Vector2(level.Width * 20, y * 20), y % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
    }

    public void ProcessGUI()
    {
        IsInWindow = ImGui.IsWindowHovered(
            ImGuiHoveredFlags.AnyWindow |
            ImGuiHoveredFlags.AllowWhenBlockedByPopup |
            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem |
            ImGuiHoveredFlags.ChildWindows
        );
    }

    public void PrintDebug()
    {
        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Zoom", Context.Camera.Zoom, Color.SkyBlue);
        printer.PrintlnLabel("X/Y", $"{X:F0}/{Y:F0}", Color.SkyBlue);
        printer.PrintlnLabel("MTX", $"{MX}/{MY}", Color.SkyBlue);
    }
}