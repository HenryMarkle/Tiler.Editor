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

public class Light : BaseView
{
    public Light(Context context) : base(context)
    {
        
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
        base.Process();
    }

    public override void Draw()
    {
        base.Draw();
    }

    public override void GUI()
    {
        base.GUI();
    }

    public override void Debug()
    {
        base.Debug();
    }
}