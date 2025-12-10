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
    }
}