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

public class Tiles : BaseView
{
    public Tiles(Context context) : base(context)
    {
        cursor = new Cursor(context);

        redrawMain = true;
        redrawTiles = new bool[context.Viewports.Depth];
        Array.Fill(redrawTiles, true);

        var dex = context.Tiles;

        if (dex.Tiles.Count > 0)
        {
            if (dex.Categories.Count > 0)
            {
                selectedTileMenuCategoryIndex = 0;
                selectedTileMenuCategory = Context.Tiles.Categories[0];
                selectedTileMenuCategoryTiles = Context.Tiles.CategoryTiles[selectedTileMenuCategory];

                if (selectedTileMenuCategoryTiles.Count > 0)
                {
                    selectedTileMenuIndex = 0;
                    selectedTile = selectedTileMenuCategoryTiles[selectedTileMenuIndex];
                }
            }
            else if (dex.UnCategorizedTiles.Count > 0)
            {
                selectedTileMenuIndex = 0;
                selectedTile = dex.UnCategorizedTiles[selectedTileMenuIndex];
            }
        }

        cursor.AreaSelected += OnAreaSelected;
    }

    ~Tiles()
    {
        cursor.AreaSelected -= OnAreaSelected;
    }

    private readonly Cursor cursor;

    private bool redrawMain;
    private bool[] redrawTiles;

    private int selectedTileMenuCategoryIndex;
    private int selectedTileMenuIndex;
    private TileDef? selectedTile;

    private string? selectedTileMenuCategory;
    private List<TileDef>? selectedTileMenuCategoryTiles;

    /// TODO: Extract
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
                        DrawRectangle(posX: x * 20 + 5, posY: y * 20 + 5, width: 20 - 10, height: 20 - 10, tile.Color);
                    break;

                    case Geo.Slab:
                        DrawRectangle(posX: x * 20 + 5, posY: y * 20 + 10 + 5, width: 20 - 10, height: 10 - 10, tile.Color);
                    break;

                    case Geo.Platform:
                        DrawRectangle(posX: x * 20 + 5, posY: y * 20 + 5, width: 20 - 10, height: 10 - 10, tile.Color);
                    break;

                    case Geo.SlopeNW:
                        DrawTriangle(
                            v1: new Vector2((x + 1) * 20, y * 20),
                            v2: new Vector2(x * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                    break;                    

                    case Geo.SlopeNE:
                        DrawTriangle(
                            v1: new Vector2(x * 20, y * 20),
                            v2: new Vector2(x * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                    break;

                    case Geo.SlopeSE:
                        DrawTriangle(
                            v1: new Vector2((x + 1) * 20, y * 20),
                            v2: new Vector2(x * 20, y * 20),
                            v3: new Vector2(x * 20, (y + 1) * 20),
                            tile.Color
                        );
                    break;

                    case Geo.SlopeSW:
                        DrawTriangle(
                            v1: new Vector2(x * 20, y * 20),
                            v2: new Vector2((x + 1) * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, y * 20),
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
        for (var l = Context.Viewports.Depth - 1; l > -1; --l)
        {
            if (l == Context.Layer) continue;
            DrawTexture(Context.Viewports.Geos[l].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 120 });
            DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 120 });
        }
        
        DrawRectangle(posX: 0, posY: 0, width: level.Width * 20, height: level.Height * 20, Color.Red with { A = 40 });

        DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 210 });
        DrawTexture(Context.Viewports.Tiles[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 210 });
        EndTextureMode();
    }

    public override void OnViewSelected()
    {
        Array.Fill(redrawTiles, true);
        redrawMain = true;
    }

    public override void OnLevelSelected(Level level)
    {
        Array.Fill(redrawTiles, true);
        redrawMain = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PlaceOne(int mx, int my, int mz, TileDef? tile)
    {
        if (Context.SelectedLevel is not { } level || !level.Tiles.IsInBounds(mx, my, mz)) return;

        level.Tiles[mx, my, mz] = tile;

        BeginTextureMode(Context.Viewports.Tiles[mz]);

        // Clear area
        BeginBlendMode(BlendMode.Custom);
        Rlgl.SetBlendMode(BlendMode.Custom);
        Rlgl.SetBlendFactors(glSrcFactor: 1, glDstFactor: 0, glEquation: 1);
        DrawRectangle(
            posX: mx * 20, posY: my * 20, width: 20, height: 20,
            new Color(0, 0, 0, 0)
        );
        EndBlendMode();

        if (tile is not null)
        {
            var geo = level.Geos[mx, my, mz];

            switch (geo)
            {
                case Geo.Solid:
                case Geo.Wall:
                    DrawRectangle(posX: mx * 20 + 5, posY: my * 20 + 5, width: 20 - 10, height: 20 - 10, tile.Color);
                break;

                case Geo.Slab:
                    DrawRectangle(posX: mx * 20 + 5, posY: my * 20 + 10 + 5, width: 20 - 10, height: 10 - 10, tile.Color);
                break;

                case Geo.Platform:
                    DrawRectangle(posX: mx * 20 + 5, posY: my * 20 + 5, width: 20 - 10, height: 10 - 10, tile.Color);
                break;

                case Geo.SlopeNW:
                    DrawTriangle(
                        v1: new Vector2((mx + 1) * 20 - 8, my * 20 + 4),
                        v2: new Vector2(mx * 20 + 4, (my + 1) * 20 - 8),
                        v3: new Vector2((mx + 1) * 20 - 8, (my + 1) * 20 - 8),
                        tile.Color
                    );
                break;                    

                case Geo.SlopeNE:
                    DrawTriangle(
                        v1: new Vector2(mx * 20 + 4, my * 20 + 8),
                        v2: new Vector2(mx * 20 + 4, (my + 1) * 20 - 4),
                        v3: new Vector2((mx + 1) * 20 - 8, (my + 1) * 20 - 4),
                        tile.Color
                    );
                break;

                case Geo.SlopeSE:
                    DrawTriangle(
                        v1: new Vector2((mx + 1) * 20 - 8, my * 20 + 4),
                        v2: new Vector2(mx * 20 + 4, my * 20 + 4),
                        v3: new Vector2(mx * 20 + 4, (my + 1) * 20 - 8),
                        tile.Color
                    );
                break;

                case Geo.SlopeSW:
                    DrawTriangle(
                        v1: new Vector2(mx * 20 + 8, my * 20 + 4),
                        v2: new Vector2((mx + 1) * 20 - 4, (my + 1) * 20 - 8),
                        v3: new Vector2((mx + 1) * 20 - 4, my * 20 + 4),
                        tile.Color
                    );
                break;
            }
        }
        EndTextureMode();

        redrawMain = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnAreaSelected(Rectangle rect, bool isErasing)
    {
        if (selectedTile is null) return;

        for (var y = (int)rect.Y; y < rect.Y + rect.Height; y++)
        for (var x = (int)rect.X; x < rect.X + rect.Width; x++)
        {
            PlaceOne(x, y, Context.Layer, isErasing ? null : selectedTile);
        }
    }

    public override void Process()
    {
        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();
            cursor.ProcessSelection();
        
            if (IsKeyPressed(KeyboardKey.L))
            {
                Context.Layer = ++Context.Layer % Context.SelectedLevel!.Depth;

                redrawMain = true;
            }

            if (cursor.IsInMatrix)
            {
                if (!cursor.IsSelecting)
                {
                    if (IsMouseButtonDown(MouseButton.Left))
                    {
                        if ((selectedTile is not null) && selectedTile != Context.SelectedLevel?.Tiles[cursor.MX, cursor.MY, Context.Layer])
                            PlaceOne(cursor.MX, cursor.MY, Context.Layer, selectedTile);
                    }
                    else if (IsMouseButtonDown(MouseButton.Right))
                    {
                        if (Context.SelectedLevel?.Tiles[cursor.MX, cursor.MY, Context.Layer] is not null)
                            PlaceOne(cursor.MX, cursor.MY, Context.Layer, tile: null);
                    }
                }
            }
            
            if (selectedTileMenuCategoryTiles is [_, ..])
            {    
                if (IsKeyPressed(KeyboardKey.W))
                {
                    selectedTileMenuIndex -= 1;
                    if (selectedTileMenuIndex < 0) selectedTileMenuIndex = selectedTileMenuCategoryTiles.Count - 1;

                    selectedTile = selectedTileMenuCategoryTiles[selectedTileMenuIndex];
                }
                else if (IsKeyPressed(KeyboardKey.S))
                {
                    selectedTileMenuIndex += 1;
                    if (selectedTileMenuIndex >= selectedTileMenuCategoryTiles.Count) selectedTileMenuIndex = 0;

                    selectedTile = selectedTileMenuCategoryTiles[selectedTileMenuIndex];
                }

                if (IsKeyPressed(KeyboardKey.A))
                {
                    selectedTileMenuCategoryIndex -= 1;
                    if (selectedTileMenuCategoryIndex < 0) selectedTileMenuCategoryIndex = Context.Tiles.Categories.Count - 1;

                    selectedTileMenuCategory = Context.Tiles.Categories[selectedTileMenuCategoryIndex];
                    selectedTileMenuCategoryTiles = Context.Tiles.CategoryTiles[selectedTileMenuCategory];

                    if (selectedTileMenuCategoryTiles is [ var first, .. ])
                    {
                        selectedTileMenuIndex = 0;
                        selectedTile = first;
                    }
                    else
                    {
                        selectedTileMenuIndex = -1;
                        selectedTile = null;
                    }
                }
                else if (IsKeyPressed(KeyboardKey.D))
                {
                    selectedTileMenuCategoryIndex += 1;
                    if (selectedTileMenuCategoryIndex >= Context.Tiles.Categories.Count) selectedTileMenuCategoryIndex = 0;

                    selectedTileMenuCategory = Context.Tiles.Categories[selectedTileMenuCategoryIndex];
                    selectedTileMenuCategoryTiles = Context.Tiles.CategoryTiles[selectedTileMenuCategory];

                    if (selectedTileMenuCategoryTiles is [ var first, .. ])
                    {
                        selectedTileMenuIndex = 0;
                        selectedTile = first;
                    }
                    else
                    {
                        selectedTileMenuIndex = -1;
                        selectedTile = null;
                    }
                }
            }
        }

    }

    public override void Draw()
    {
        if (Context.SelectedLevel is null) return;

        var level = Context.SelectedLevel;

        for (var l = 0; l < Context.Viewports.Depth; l++)
        {
            if (redrawTiles[l]) {
                DrawTilesViewport(l);
                redrawMain = true;
            }
        }
        Array.Fill(redrawTiles, false);

        if (redrawMain)
        {
            DrawMainViewport();
            redrawMain = false;
        }

        ref var camera = ref Context.Camera;
        
        BeginMode2D(camera);
        DrawTexture(Context.Viewports.Main.Raw.Texture, posX: 0, posY: 0, tint: Color.White);
        DrawRectangleLinesEx(new Rectangle(0, 0, width: level.Width *20f, height: level.Height * 20f), lineThick: 4, Color.Black);
        DrawRectangleLinesEx(new Rectangle(0, 0, width: level.Width *20f, height: level.Height * 20f), lineThick: 2, Color.White);

        cursor.DrawCursor();
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin(name: "Tiles"))
        {
            var level = Context.SelectedLevel;

            if (level is null || selectedTile is null) ImGui.BeginDisabled();
            var isDefault = (selectedTile is not null) && selectedTile == level?.DefaultTile;
            if (level is null || selectedTile is null) ImGui.EndDisabled();

            if (ImGui.Checkbox(label: "Default Tile", ref isDefault))
            {
                if (isDefault)
                {
                    level!.DefaultTile = selectedTile;
                }
                else
                {
                    level!.DefaultTile = null;
                }
            }
        
            var avail = ImGui.GetContentRegionAvail();

            if (ImGui.BeginListBox(label: "##Categories", new Vector2(avail.X/2, avail.Y)))
            {
                for (var c = 0; c < Context.Tiles.Categories.Count; c++)
                {
                    var category = Context.Tiles.Categories[c];

                    if (ImGui.Selectable(label: $"{category}##{c}", selected: selectedTileMenuCategoryIndex == c))
                    {
                        selectedTileMenuCategory = category;
                        selectedTileMenuCategoryTiles = Context.Tiles.CategoryTiles[category];

                        if (selectedTileMenuCategoryTiles is [ var first, .. ])
                        {
                            selectedTileMenuIndex = 0;
                            selectedTile = first;
                        }
                        else
                        {
                            selectedTileMenuIndex = -1;
                            selectedTile = null;
                        }
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.SameLine();

            if (ImGui.BeginListBox(label: "##Tiles", size: ImGui.GetContentRegionAvail()))
            {
                var drawl = ImGui.GetWindowDrawList();
                var textHeight = ImGui.GetTextLineHeight();

                for (var t = 0; t < (selectedTileMenuCategoryTiles?.Count ?? 0); t++)
                {
                    var tile = selectedTileMenuCategoryTiles![t];
                    var pos = ImGui.GetCursorScreenPos();
                    drawl.AddRectFilled(
                        pos, 
                        pos + Vector2.One * textHeight, 
                        col: ImGui.ColorConvertFloat4ToU32(tile.Color)
                    );

                    ImGui.SetCursorScreenPos(pos with { X = pos.X + textHeight + 6 });

                    if (ImGui.Selectable(label: $"{tile.Name}##{tile.ID}", selected: selectedTileMenuIndex == t))
                    {
                        selectedTileMenuIndex = t;
                        selectedTile = tile;
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

        if (Context.SelectedLevel is { } level)
        {
            var tile = level.Tiles[cursor.MX, cursor.MY, Context.Layer];

            printer.PrintlnLabel("Layer", Context.Layer, Color.Magenta);
            
            printer.PrintlnLabel("Tile", tile?.ToString() ?? "NULL", Color.Gold);
        }
    }
}