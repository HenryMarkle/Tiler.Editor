namespace Tiler.Editor.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;
using Tiler.Editor.Managed;
using Tiler.Editor.Tile;
using Tiler.Editor.Views.Components;
using static Raylib_cs.Raylib;

public class Props : BaseView
{
    private readonly Cursor cursor;
    private int selectedPropMenuCategoryIndex;
    private string? selectedPropMenuCategory;
    private List<PropDef>? selectedPropMenuCategoryProps;
    private int selectedPropMenuIndex;
    private PropDef? selectedProp;

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

        if (ImGui.Begin("Props"))
        {
            ImGui.Columns(2);

            if (ImGui.BeginListBox("##Categories", ImGui.GetContentRegionAvail()))
            {
                for (var c = 0; c < Context.Props.Categories.Count; c++)
                {
                    var category = Context.Props.Categories[c];
                    if (ImGui.Selectable(category, c == selectedPropMenuCategoryIndex))
                    {
                        selectedPropMenuCategoryIndex = c;
                        selectedPropMenuCategory = category;
                        selectedPropMenuCategoryProps = Context.Props.CategoryProps[category];
                        selectedPropMenuIndex = 0;
                        if (selectedPropMenuCategoryProps.Count > 0) 
                            selectedProp = selectedPropMenuCategoryProps[0];
                    }
                }
                
                ImGui.EndListBox();
            }

            ImGui.NextColumn();

            if (ImGui.BeginListBox("##Props", ImGui.GetContentRegionAvail()))
            {
                if (selectedPropMenuCategoryProps is not null)
                {    
                    for (var p = 0; p < selectedPropMenuCategoryProps.Count; p++)
                    {
                        var prop = selectedPropMenuCategoryProps[p];

                        if (ImGui.Selectable($"{prop.Name ?? prop.ID}##{prop.ID}", selectedPropMenuIndex == p))
                        {
                            selectedPropMenuIndex = p;
                            selectedProp = prop;
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();
    }
}