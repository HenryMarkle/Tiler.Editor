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

    private bool redrawMain;

    public enum Precision
    {
        Free,
        Half,
        One
    }

    private Precision transformPrecision;
    private Precision gridPrecision;

    private Vector2 TransPos => transformPrecision switch
    {
        Precision.Half => new((int)(cursor.X / 10) * 10, (int)(cursor.Y / 10) * 10),
        Precision.One => new((int)(cursor.X / 20) * 20, (int)(cursor.Y / 20) * 20),
        _ => cursor.Pos,
    };

    private enum EditMode
    {
        Selection,
        Placement
    }

    private EditMode editMode;

    private bool isSelecting;
    private Vector2 initialSelectionPos;
    private Rectangle selectionRect;

    public Props(Context context) : base(context)
    {
        cursor = new Cursor(context);

        transformPrecision = gridPrecision = Precision.Free;
        editMode = EditMode.Placement;

        propPreview = new RenderTexture(
            width: 1,
            height: 1,
            clearColor: new Color4(0, 0, 0, 0),
            clear: true
        );
        propTooltip = new RenderTexture(
            width: 1,
            height: 1,
            clearColor: new Color4(0, 0, 0, 0),
            clear: true
        );

        SelectPropCategory(0);
        if (selectedProp is not null) DrawPropRT(propPreview, selectedProp);
    }

    private void SelectPropCategory(int index)
    {
        if (index >= Context.Props.Categories.Count) return;
        selectedPropMenuCategoryIndex = index;
        selectedPropMenuCategory = Context.Props.Categories[index];
        selectedPropMenuCategoryProps = Context.Props.CategoryProps[selectedPropMenuCategory];
        SelectPropFromCategory(0);
    }
    private void SelectPropCategory(string category)
    {
        if (!Context.Props.CategoryProps.TryGetValue(category, out selectedPropMenuCategoryProps)) return;
        selectedPropMenuCategoryIndex = Context.Props.Categories.IndexOf(category);
        selectedPropMenuCategory = category;
        SelectPropFromCategory(0);
    }

    private void SelectPropFromCategory(int index)
    {
        if (selectedPropMenuCategoryProps is null or { Count: 0 }) return;
        if (index >= selectedPropMenuCategoryProps.Count) return;

        selectedPropMenuIndex = index;
        selectedProp = selectedPropMenuCategoryProps[index];
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
                            source: new Rectangle(0, l * voxels.Height, voxels.Width, voxels.Height),
                            destination: new Rectangle(0, 0, voxels.Width, voxels.Height),
                            tint: Color.White with { A = (byte)(255 - l) }
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
                        source: new Rectangle(0, 0, soft.Width, soft.Height),
                        destination: new Rectangle(0, 0, soft.Width, soft.Height),
                        tint: Color.White
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
                        source: new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                        destination: new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                        tint: Color.White
                    );
                }
                break;

            default: return;
        }
    }

    public void DrawTilesViewport(int layer)
    {
        if (Context.SelectedLevel is not { } level) return;
        if (layer < 0 || layer >= Context.Viewports.Depth) return;

        BeginTextureMode(Context.Viewports.Tiles[layer]);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                var tile = level.Tiles[x, y, layer];
                if (tile is null) continue;

                var geo = level.Geos[x, y, layer];

                // BeginBlendMode(BlendMode.Custom);
                // Rlgl.SetBlendMode(BlendMode.Custom);
                // Rlgl.SetBlendFactors(1, 0, 1);

                switch (geo)
                {
                    case Geo.Solid:
                    case Geo.Wall:
                        DrawRectangle(x * 20 + 4, y * 20 + 4, 20 - 8, 20 - 8, tile.Color);
                        break;

                    case Geo.Slab:
                        DrawRectangle(x * 20 + 4, y * 20 + 10 + 4, 20 - 8, 10 - 8, tile.Color);
                        break;

                    case Geo.Platform:
                        DrawRectangle(x * 20 + 4, y * 20 + 4, 20 - 8, 10 - 8, tile.Color);
                        break;

                    case Geo.SlopeNW:
                        DrawTriangle(
                            new Vector2((x + 1) * 20, y * 20),
                            new Vector2(x * 20, (y + 1) * 20),
                            new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeNE:
                        DrawTriangle(
                            new Vector2(x * 20, y * 20),
                            new Vector2(x * 20, (y + 1) * 20),
                            new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeSE:
                        DrawTriangle(
                            new Vector2((x + 1) * 20, y * 20),
                            new Vector2(x * 20, y * 20),
                            new Vector2(x * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeSW:
                        DrawTriangle(
                            new Vector2(x * 20, y * 20),
                            new Vector2((x + 1) * 20, (y + 1) * 20),
                            new Vector2((x + 1) * 20, y * 20),
                            tile.Color
                        );
                        break;
                }
                // EndBlendMode();
            }
        }
        EndTextureMode();
    }

    public void DrawMainViewport()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Main);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int l = 0; l < Context.Viewports.Depth; l++)
        {
            if (l == Context.Layer) continue;
            DrawTexture(Context.Viewports.Geos[l].Raw.Texture, 0, 0, Color.Black with { A = 120 });
            DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, 0, 0, Color.White with { A = 120 });
        }

        DrawRectangle(0, 0, level.Width * 20, level.Height * 20, Color.Red with { A = 40 });

        DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, 0, 0, Color.Black with { A = 210 });
        DrawTexture(Context.Viewports.Tiles[Context.Layer].Raw.Texture, 0, 0, Color.White with { A = 210 });
        EndTextureMode();
    }

    public void UpdatePlacedPropPreview(Prop prop)
    {
        throw new NotImplementedException();
    }

    public void DrawPlacedProps()
    {
        throw new NotImplementedException();
    }

    public override void OnViewSelected()
    {
        redrawMain = true;
    }

    public override void Process()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();

            if (IsKeyPressed(KeyboardKey.L))
            {
                Context.Layer = ++Context.Layer % Context.SelectedLevel?.Depth ?? 3;
                redrawMain = true;
            }

            if (IsKeyPressed(KeyboardKey.P))
            {
                transformPrecision = transformPrecision switch
                {
                    Precision.Free => Precision.Half,
                    Precision.Half => Precision.One,
                    Precision.One => Precision.Free,

                    _ => Precision.Free
                };
            }

            if (IsKeyPressed(KeyboardKey.G))
            {
                gridPrecision = gridPrecision switch
                {
                    Precision.Free => Precision.One,
                    Precision.One => Precision.Half,
                    Precision.Half => Precision.Free,

                    _ => Precision.Free
                };
            }

            switch (editMode)
            {
                case EditMode.Placement:
                placement_mode_case:
                    {
                        // Avoid loop
                        if (IsMouseButtonDown(MouseButton.Left) && !IsMouseButtonDown(MouseButton.Right))
                        {
                            editMode = EditMode.Selection;
                            goto selection_mode_case;
                        }

                        if (IsMouseButtonPressed(MouseButton.Right))
                        {
                            if (selectedProp is null) break;

                            var previewSize = new Vector2(propPreview.Width, propPreview.Height);

                            var quad = new Quad(
                                TransPos - (previewSize / 2),
                                TransPos + (Vector2.UnitX * (previewSize.X / 2)),
                                TransPos + (previewSize / 2),
                                TransPos + (Vector2.UnitY * (previewSize.Y / 2))
                            );

                            var prop = new Prop
                            {
                                Def = selectedProp,
                                Config = selectedProp.CreateConfig(),
                                Quad = quad,
                                Preview = level.Props.Find(p => p.Def == selectedProp) is { } replica
                                    ? replica.Preview
                                    : new Managed.Image(propPreview.Texture)
                            };

                            level.Props.Add(prop);
                        }
                    }
                    break;

                case EditMode.Selection:
                selection_mode_case:
                    {
                        // Avoid loop
                        if (IsMouseButtonPressed(MouseButton.Right) && !IsMouseButtonDown(MouseButton.Left))
                        {
                            editMode = EditMode.Placement;
                            break;
                        }

                        if (isSelecting)
                        {
                            if (IsMouseButtonDown(MouseButton.Left))
                            {
                                var minX = MathF.Min(initialSelectionPos.X, cursor.X);
                                var minY = MathF.Min(initialSelectionPos.Y, cursor.Y);
                                
                                var maxX = MathF.Max(initialSelectionPos.X, cursor.X);
                                var maxY = MathF.Max(initialSelectionPos.Y, cursor.Y);

                                selectionRect.X = minX;
                                selectionRect.Y = minY;
                                selectionRect.Width = maxX - minX;
                                selectionRect.Height = maxY - minY;
                            }
                            else if (IsMouseButtonReleased(MouseButton.Left))
                            {
                                isSelecting = false;
                                initialSelectionPos = -Vector2.One;
                                selectionRect = new Rectangle(-1, -1, -1, -1);
                            }
                        }
                        else
                        {
                            if (IsMouseButtonDown(MouseButton.Left))
                            {
                                isSelecting = true;
                                initialSelectionPos = cursor.Pos;
                                selectionRect = new Rectangle(initialSelectionPos, Vector2.One);
                            }
                            else if (IsMouseButtonReleased(MouseButton.Left))
                            {

                            }
                        }
                    }
                    break;
            }
        }
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is not { } level) return;
        if (redrawMain)
        {
            DrawTilesViewport(0);
            DrawTilesViewport(1);
            DrawTilesViewport(2);
            DrawMainViewport();

            redrawMain = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(
            texture: Context.Viewports.Main.Raw.Texture,
            posX: 0,
            posY: 0,
            tint: Color.White
        );

        switch (gridPrecision)
        {
            case Precision.One:
                for (var x = 0; x < level.Width; x++)
                    DrawLineEx(new Vector2(x * 20, 0), new Vector2(x * 20, level.Height * 20), x % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
                for (var y = 0; y < level.Height; y++)
                    DrawLineEx(new Vector2(0, y * 20), new Vector2(level.Width * 20, y * 20), y % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
                break;

            case Precision.Half:
                for (var x = 0; x < level.Width * 2; x++)
                    DrawLineEx(new Vector2(x * 10, 0), new Vector2(x * 10, level.Height * 20), x % 2 == 0 ? 1 : 0.5f, Color.White with { A = 80 });
                for (var y = 0; y < level.Height * 2; y++)
                    DrawLineEx(new Vector2(0, y * 10), new Vector2(level.Width * 20, y * 10), y % 2 == 0 ? 1 : 0.5f, Color.White with { A = 80 });
                break;
        }

        switch (editMode)
        {
            case EditMode.Placement:
                {
                    if (selectedProp is not null)
                    {
                        var previewSize = new Vector2(propPreview.Width, propPreview.Height);

                        DrawTexturePro(
                            texture: propPreview.Texture,
                            source: new Rectangle(0, 0, previewSize),
                            dest: new Rectangle(TransPos, previewSize),
                            origin: previewSize / 2,
                            rotation: 0,
                            tint: Color.White
                        );
                    }
                }
                break;

            case EditMode.Selection:
                {
                    if (isSelecting)
                    {
                        DrawRectangleLinesEx(
                            rec:       selectionRect,
                            lineThick: 1f,
                            color:     Color.SkyBlue
                        );
                    }
                }
                break;
        }

        EndMode2D();
    }

    public override void GUI()
    {
        if (Context.SelectedLevel is not { } level) return;

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
                            rlImGui_cs.rlImGui.Image(propTooltip.Texture);
                            ImGui.EndTooltip();
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin("Placed##PlacedProps"))
        {
            if (ImGui.BeginListBox("##List", ImGui.GetContentRegionAvail()))
            {
                for (var p = 0; p < level.Props.Count; p++)
                {
                    var prop = level.Props[p];

                    if (ImGui.Selectable($"{prop.Def.ID}##{p}"))
                    {

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

        printer.PrintlnLabel("Precision", transformPrecision, Color.Magenta);
        printer.PrintlnLabel("Selected Category", selectedPropMenuCategory, Color.Gold);
        printer.PrintlnLabel("Selected Prop", selectedProp, Color.Gold);
    }
}