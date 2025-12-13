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

public class Props : BaseView
{
    private readonly Cursor cursor;
    public Props(Context context) : base(context)
    {
        cursor = new Cursor(context);
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
        BeginMode2D(Context.Camera);
        DrawTexture(
            texture: Context.Viewports.Main.Raw.Texture, 
            posX:    0, 
            posY:    0, 
            tint:    Color.White
        );
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