namespace Tiler.Editor.Views;

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Raylib_cs;
using Tiler.Editor.Managed;
using Tiler.Editor.Views.Components;
using static Raylib_cs.Raylib;

public class Connections : BaseView
{
    private readonly Cursor cursor;

    private bool drawGrid;

    public Connections(Context context) : base(context)
    {
        cursor = new Cursor(context);
    }

    public override void OnViewSelected()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Main);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int l = Context.Viewports.Depth - 1; l > 0; --l)
        {
            DrawTexture(Context.Viewports.Geos[l].Raw.Texture, 0, 0, Color.Black with { A = 120 });
            DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, 0, 0, Color.White with { A = 120 });
        }
        
        DrawRectangle(0, 0, level.Width * 20, level.Height * 20, Color.Red with { A = 40 });

        DrawTexture(Context.Viewports.Geos[0].Raw.Texture, 0, 0, Color.Black with { A = 210 });
        DrawTexture(Context.Viewports.Tiles[0].Raw.Texture, 0, 0, Color.White with { A = 210 });
        EndTextureMode();
    }

    public override void Process()
    {
        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();
        }

        if (IsKeyPressed(KeyboardKey.G)) 
            drawGrid = !drawGrid;
    }

    public override void Draw()
    {
        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, 0, 0, Color.White);

        if (drawGrid) cursor.DrawGrid();
        cursor.DrawCursor();
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