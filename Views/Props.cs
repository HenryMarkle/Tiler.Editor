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
    private PropDef? hoveredProp;

    RenderTexture propPreview;
    RenderTexture propTooltip;

    public Props(Context context) : base(context)
    {
        cursor = new Cursor(context);

        propPreview = new RenderTexture(
            width:      1, 
            height:     1, 
            clearColor: new Color4(0, 0, 0, 0), 
            clear:      true
        );
        propTooltip = new RenderTexture(
            width:      1, 
            height:     1, 
            clearColor: new Color4(0, 0, 0, 0), 
            clear:      true
        );
    }

    public void DrawPropRT(RenderTexture rt, PropDef prop)
    {
        rt.Clear();

        switch (prop)
        {
            case VoxelStruct voxels:
            {
                if (rt.Width != voxels.Width || rt.Height != voxels.Height)
                    rt.CleanResize(voxels.Width, voxels.Height);

                using var texture = new Texture(voxels.Image);

                for (var l = voxels.Layers - 1; l > -1; l--)
                {
                    RlUtils.DrawTextureRT(
                        rt,
                        texture,
                        source:      new Rectangle(0, l * voxels.Height, voxels.Width, voxels.Height),
                        destination: new Rectangle(0, 0, voxels.Width, voxels.Height),
                        tint:        Color.White with { A = (byte)(255 - l) }
                    );
                }
            }
            break;

            case Soft soft:
            {
                if (rt.Width != soft.Width || rt.Height != soft.Height)
                    rt.CleanResize(soft.Width, soft.Height);

                using var texture = new Texture(soft.Image);

                RlUtils.DrawTextureRT(
                    rt,
                    texture,
                    source:      new Rectangle(0, 0, soft.Width, soft.Height),
                    destination: new Rectangle(0, 0, soft.Width, soft.Height),
                    tint:        Color.White
                );
            }
            break;
            
            case Antimatter antimatter:
            {
                if (rt.Width != antimatter.Width || rt.Height != antimatter.Height)
                    rt.CleanResize(antimatter.Width, antimatter.Height);

                using var texture = new Texture(antimatter.Image);

                RlUtils.DrawTextureRT(
                    rt,
                    texture,
                    source:      new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                    destination: new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                    tint:        Color.White
                );
            }
            break;

            default: return;
        }
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
                            if (prop != selectedProp)
                            {
                                DrawPropRT(propPreview, prop);    
                            }

                            selectedPropMenuIndex = p;
                            selectedProp = prop;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            if (prop != hoveredProp)
                            {
                                DrawPropRT(propTooltip, prop);
                                hoveredProp = prop;
                            }

                            ImGui.BeginTooltip();
                            rlImGui_cs.rlImGui.ImageSize(propTooltip.Texture, new Vector2(100, 100));
                            ImGui.EndTooltip();
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

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Layer", Context.Layer, Color.Magenta);

        printer.PrintlnLabel("Selected Category", selectedPropMenuCategory, Color.Gold);
        printer.PrintlnLabel("Selected Prop", selectedProp, Color.Gold);
    }
}