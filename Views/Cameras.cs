namespace Tiler.Editor.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Raylib_cs;
using Tiler.Editor.Managed;
using Tiler.Editor.Tile;
using Tiler.Editor.Views.Components;
using static Raylib_cs.Raylib;

public class Cameras : BaseView
{
    private readonly Texture cameraSprite;
    private readonly Cursor cursor;
    private LevelCamera? selectedCamera;
    private static readonly Color[] cameraColors = [
        Color.Green,
        Color.Red,
        Color.Blue,
        Color.Magenta,
        Color.Orange,
        Color.Gold,
        Color.Gray,
    ];

    private LevelCamera? quadCamLock;
    private int quadLock;

    public Cameras(Context context) : base(context)
    {
        cameraSprite = new Texture(LoadTexture(context.Dirs.Files.CameraSprite));
        cursor = new Cursor(context);
    }
    

    public override void OnLevelSelected(Level level)
    {
        base.OnLevelSelected(level);
    }

    public override void OnViewSelected()
    {
        base.OnViewSelected();
    }


    public override void Process()
    {
        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();

            if (Context.SelectedLevel is { } level)
            {
                if (selectedCamera is null)
                {
                    if (IsKeyPressed(KeyboardKey.N))
                    {
                        var ncam = new LevelCamera(new Vector2(LevelCamera.Width/2 - cursor.X, LevelCamera.Height/2 - cursor.Y));

                        level.Cameras.Add(ncam);
                        selectedCamera = ncam;
                    }

                    for (var c = level.Cameras.Count - 1; c >= 0; --c)
                    {
                        var cam = level.Cameras[c];

                        var center = cam.Position + (new Vector2(LevelCamera.Width, LevelCamera.Height) / 2);

                        var tl = cam.Position + cam.TopLeft.Position;
                        var tr = cam.Position + (Vector2.UnitX*LevelCamera.Width) + cam.TopRight.Position;
                        var br = cam.Position + new Vector2(LevelCamera.Width, LevelCamera.Height) + cam.BottomRight.Position;
                        var bl = cam.Position + (Vector2.UnitY*LevelCamera.Height) + cam.BottomLeft.Position;

                        if (selectedCamera is null)
                        {
                            if (
                                CheckCollisionPointCircle(new Vector2(cursor.X, cursor.Y), center, 110) && 
                                IsMouseButtonPressed(MouseButton.Left)
                            ) {
                                selectedCamera = cam;
                            }
                            else
                            {
                                if (cam == quadCamLock)
                                {
                                    switch (quadLock)
                                    {
                                        case 1: cam.TopLeft.Position = cursor.Pos - cam.Position; break;
                                        case 2: cam.TopRight.Position = cursor.Pos - cam.Position - (Vector2.UnitX*LevelCamera.Width); break;
                                        case 3: cam.BottomRight.Position = cursor.Pos - cam.Position - new Vector2(LevelCamera.Width, LevelCamera.Height); break;
                                        case 4: cam.BottomLeft.Position = cursor.Pos - cam.Position - (Vector2.UnitY*LevelCamera.Height); break;
                                    }

                                    if (quadLock != 0 && IsMouseButtonPressed(MouseButton.Left))
                                    {
                                        quadLock = 0;
                                        quadCamLock = null;
                                    }
                                }
                                else
                                {
                                    if (CheckCollisionPointCircle(cursor.Pos, tl, 10) && IsMouseButtonPressed(MouseButton.Left))
                                        quadLock = 1;
                                    else if (CheckCollisionPointCircle(cursor.Pos, tr, 10) && IsMouseButtonPressed(MouseButton.Left))
                                        quadLock = 2;
                                    else if (CheckCollisionPointCircle(cursor.Pos, br, 10) && IsMouseButtonPressed(MouseButton.Left))
                                        quadLock = 3;
                                    else if (CheckCollisionPointCircle(cursor.Pos, bl, 10) && IsMouseButtonPressed(MouseButton.Left))
                                        quadLock = 4;

                                    if (quadLock != 0) quadCamLock = cam;
                                }
                            }
                        }
                    }
                }
                else
                {
                    selectedCamera.Position.X = cursor.X - LevelCamera.Width / 2;
                    selectedCamera.Position.Y = cursor.Y - LevelCamera.Height / 2;
                
                    if (IsMouseButtonPressed(MouseButton.Left))
                    {
                        selectedCamera = null;
                    }
                    else if (IsKeyPressed(KeyboardKey.D))
                    {
                        level.Cameras.Remove(selectedCamera);
                        selectedCamera = null;
                    }
                }
            }

        }
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is not { } level) return;

        var vp = Context.Viewports;

        BeginMode2D(Context.Camera);
        DrawTexture(vp.Main.Raw.Texture, 0, 0, Color.White);

        for (var c = 0; c < level.Cameras.Count; c++)
        {
            var cam = level.Cameras[c];

            var color = cameraColors[c % cameraColors.Length];

            DrawRectangleV(cam.Position, new Vector2(LevelCamera.Width, LevelCamera.Height), color with { A = 50 });
            DrawTextureV(cameraSprite, cam.Position, Color.White);
            DrawText($"{c}", (int)(cam.Position.X + 25), (int)(cam.Position.Y + 20), 20, Color.White);

            var tl = cam.Position + cam.TopLeft.Position;
            var tr = cam.Position + (Vector2.UnitX*LevelCamera.Width) + cam.TopRight.Position;
            var br = cam.Position + new Vector2(LevelCamera.Width, LevelCamera.Height) + cam.BottomRight.Position;
            var bl = cam.Position + (Vector2.UnitY*LevelCamera.Height) + cam.BottomLeft.Position;

            DrawLineEx(tl, tr, 1, color);
            DrawLineEx(tr, br, 1, color);
            DrawLineEx(br, bl, 1, color);
            DrawLineEx(bl, tl, 1, color);

            DrawLineEx(cam.Position, tl, 1, color);
            DrawLineEx(cam.Position + (Vector2.UnitX*LevelCamera.Width), tr, 1, color);
            DrawLineEx(cam.Position + new Vector2(LevelCamera.Width, LevelCamera.Height), br, 1, color);
            DrawLineEx(cam.Position + (Vector2.UnitY*LevelCamera.Height), bl, 1, color);

            DrawCircleV(
                center: tl,
                radius: 12,
                color: Color.White with { A = 180 }
            );
            DrawCircleV(
                center: tr,
                radius: 12,
                color: Color.White with { A = 180 }
            );
            DrawCircleV(
                center: br,
                radius: 12,
                color: Color.White with { A = 180 }
            );
            DrawCircleV(
                center: bl,
                radius: 12,
                color: Color.White with { A = 180 }
            );

            if (CheckCollisionPointCircle(cursor.Pos, cam.Position + (new Vector2(LevelCamera.Width, LevelCamera.Height) / 2), 50))
            {
                DrawCircleV(cam.Position + (new Vector2(LevelCamera.Width, LevelCamera.Height) / 2), 50, Color.White with { A = 50 });
            }
        }
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Selected", selectedCamera?.Position, Color.Magenta);
    }
}