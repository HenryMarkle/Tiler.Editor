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
        Color.Green with { A = 80 },
        Color.Red with { A = 80 },
        Color.Blue with { A = 80 },
        Color.Magenta with { A = 80 },
        Color.Orange with { A = 80 },
        Color.Gold with { A = 80 },
        Color.Gray with { A = 80 },
    ];

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

                        if (selectedCamera is null)
                        {
                            if (
                                CheckCollisionPointCircle(new Vector2(cursor.X, cursor.Y), center, 110) && 
                                IsMouseButtonPressed(MouseButton.Left)
                            ) {
                                selectedCamera = cam;
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

            DrawRectangleV(cam.Position, new Vector2(LevelCamera.Width, LevelCamera.Height), cameraColors[c % cameraColors.Length]);
            DrawTextureV(cameraSprite, cam.Position, Color.White);
            DrawText($"{c}", (int)(cam.Position.X + 25), (int)(cam.Position.Y + 20), 20, Color.White);
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