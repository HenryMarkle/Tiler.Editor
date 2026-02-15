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

public class Geos : BaseView
{
    public Geos(Context context) : base(context)
    {
        Keybinds = new GeosKeybinds();
        
        redrawMain = true;
        redrawGeos = new bool[context.Viewports.Depth];
        Array.Fill(redrawGeos, true);

        atlas = new GeoAtlas(
            new Texture(
                LoadTexture(Path.Combine(Context.Dirs.Textures, "geometry", "atlas.png"))));

        cursor = new Cursor(context);

        cursor.AreaSelected += OnAreaSelected;
        geosMenu = new RenderTexture(LoadRenderTexture(width: 40, height: 140));
        selectedGeoIndex = (x: 0, y: 0);
        selectedGeo = Geo.Solid;
        
        DrawGeosMenu();

        memory = new Geo[0, 0];
    }

    ~Geos()
    {
        cursor.AreaSelected -= OnAreaSelected;
    }

    private GeoAtlas atlas;
    private bool redrawMain;
    private bool[] redrawGeos;

    private readonly Cursor cursor;

    private RenderTexture geosMenu;

    private (int x, int y) selectedGeoIndex;
    private Geo selectedGeo;

    private int brushRadius;
    private int brushCorner;

    private Geo[,] memory;
    private bool isPasting;

    /// <summary>
    /// Checks if a coordinate fits in the brush
    /// </summary>
    /// <param name="mx">Matrix X coordinate</param>
    /// <param name="my">Matrix Y coordinate</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsInBrush(int mx, int my) => CheckCollisionPointCircle(
                    point: new Vector2(mx, my) * 20 + (Vector2.One * 10), 
                    center: (cursor.MXPos * 20) + (Vector2.One * 10), 
                    radius: (brushRadius + brushCorner) * 20 + 10
                ) 
                && CheckCollisionPointRec(
                    point: new Vector2(mx + .5f, my + .5f) * 20, 
                    rec: new Rectangle(
                        position: (cursor.MXPos - (Vector2.One * brushRadius)) * 20, 
                        size: ((Vector2.One * brushRadius * 2) + Vector2.One) * 20
                    )
                );

    private void DrawBrush()
    {
        if (brushRadius == 0)
        {
            cursor.DrawCursor();
            return;
        }

        for (var x = cursor.MX - brushRadius; x < cursor.MX + brushRadius + 1; x++)
            for (var y = cursor.MY - brushRadius; y < cursor.MY + brushRadius + 1; y++)
            {
                if (!IsInBrush(x, y)) continue;

                var left   = IsInBrush(x - 1, y);
                var top    = IsInBrush(x, y - 1);
                var right  = IsInBrush(x + 1, y);
                var bottom = IsInBrush(x, y + 1);

                if (left && top && right && bottom) continue;
                if (!left && !top && !right && !bottom) continue;

                if (!left)
                    DrawLineEx(new Vector2(x, y) * 20, new Vector2(x, y + 1) * 20, thick: 1f, Color.White);
                
                if (!top)
                    DrawLineEx(new Vector2(x, y) * 20, new Vector2(x + 1, y) * 20, thick: 1f, Color.White);

                if (!right)
                    DrawLineEx(new Vector2(x + 1, y) * 20, new Vector2(x + 1, y + 1) * 20, thick: 1f, Color.White);

                if (!bottom)
                    DrawLineEx(new Vector2(x, y + 1) * 20, new Vector2(x + 1, y + 1) * 20, thick: 1f, Color.White);
            }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle GeoMenuRect(Geo g) => g switch
    {
        Geo.Solid => new Rectangle(0, 0, 20, 20),
        Geo.Air => new Rectangle(20, 0, 20, 20),
        Geo.Slab => new Rectangle(20, 20, 20, 20),
        Geo.Wall => new Rectangle(0, 20, 20, 20),
        Geo.Platform => new Rectangle(20, 40, 20, 20),
        Geo.Glass => new Rectangle(0, 40, 20, 20),
        Geo.Exit => new Rectangle(20, 60, 20, 20),
        Geo.VerticalPole => new Rectangle(0, 60, 20, 20),
        Geo.CrossPole => new Rectangle(20, 80, 20, 20),
        Geo.HorizontalPole => new Rectangle(0, 80, 20, 20),
        Geo.SlopeNW => new Rectangle(0, 100, 20, 20),
        Geo.SlopeNE => new Rectangle(20, 100, 20, 20),
        Geo.SlopeSW => new Rectangle(0, 120, 20, 20),
        Geo.SlopeSE => new Rectangle(20, 120, 20, 20),
        
        _ => new Rectangle(0, 0, 20, 20),
    };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Geo GeoMenuIndex(int x, int y) => x switch
    {
        0 => y switch
        {
            0 => Geo.Solid,
            1 => Geo.Wall,
            2 => Geo.Glass,
            3 => Geo.VerticalPole,
            4 => Geo.HorizontalPole,
            5 => Geo.SlopeNW,
            6 => Geo.SlopeSW,
            _ => Geo.Solid,
        },
        1 => y switch
        {
            0 => Geo.Air,
            1 => Geo.Slab,
            2 => Geo.Platform,
            3 => Geo.Exit,
            4 => Geo.CrossPole,
            5 => Geo.SlopeNE,
            6 => Geo.SlopeSE,
            _ => Geo.Solid,
        },
        _ => Geo.Solid,
    };

    private void DrawGeosMenu()
    {
        BeginTextureMode(geosMenu);
        ClearBackground(new Color(0, 0, 0, 0));

        for (var x = 0; x < 14; x++)
            DrawTexturePro(
                atlas.Texture,
                source: GeoAtlas.GetRect(atlas.GetIndex((Geo)x)),
                dest: GeoMenuRect((Geo)x), 
                origin: Vector2.Zero, 
                rotation: 0, 
                tint: Color.White
            );

        DrawRectangleLinesEx(
            new Rectangle(
                20 * selectedGeoIndex.x, 
                20 * selectedGeoIndex.y, 
                20, 
                20
                ), 
            lineThick: 1.0f, 
            Color.Red);

        EndTextureMode();
    }

    private void DrawGeosViewport(int layer)
    {
        if (Context.SelectedLevel is null) return;

        var level = Context.SelectedLevel;

        var vp = Context.Viewports.Geos[layer];

        BeginTextureMode(vp);
        ClearBackground(new Color(0, 0, 0, 0));
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var geo = level.Geos[x, y, layer];

                var sourceRect = GeoAtlas.GetRect(atlas.GetIndex(geo));

                BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(glSrcFactor: 1, glDstFactor: 0, glEquation: 1);
                DrawTextureRec(
                    texture: atlas.Texture, 
                    source: sourceRect, 
                    position: new Vector2(x * 20, y *20), 
                    tint: Color.White
                );
                EndBlendMode();
            }
        }
        EndTextureMode();
    }

    private void DrawMainViewport()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Main);
        switch (Context.Config.GeoColoring)
        {
            case GeometryLayerColoring.Purple:
                ClearBackground(Color.Gray);
                DrawTexture(Context.Viewports.Geos[0].Raw.Texture, posX: 0, posY: 0, new Color(0, 255, 255, 255));
                DrawTexture(Context.Viewports.Geos[1].Raw.Texture, posX: 0, posY: 0, new Color(255, 0, 255, 80));
                DrawTexture(Context.Viewports.Geos[2].Raw.Texture, posX: 0, posY: 0, new Color(255, 0, 0, 80));
                DrawTexture(Context.Viewports.Geos[3].Raw.Texture, posX: 0, posY: 0, new Color(255, 255, 0, 80));
                DrawTexture(Context.Viewports.Geos[4].Raw.Texture, posX: 0, posY: 0, new Color(0, 255, 255, 80));
            break;

            case GeometryLayerColoring.RGB:
                ClearBackground(Color.Gray);
                DrawTexture(Context.Viewports.Geos[0].Raw.Texture, posX: 0, posY: 0, new Color(0, 0, 0, 255));
                DrawTexture(Context.Viewports.Geos[1].Raw.Texture, posX: 0, posY: 0, new Color(0, 255, 0, 80));
                DrawTexture(Context.Viewports.Geos[2].Raw.Texture, posX: 0, posY: 0, new Color(255, 0, 0, 80));
                DrawTexture(Context.Viewports.Geos[3].Raw.Texture, posX: 0, posY: 0, new Color(0, 0, 255, 80));
                DrawTexture(Context.Viewports.Geos[4].Raw.Texture, posX: 0, posY: 0, new Color(200, 200, 255, 80));
            break;

            case GeometryLayerColoring.Gray:
                ClearBackground(new Color(0, 0, 0, 0));
                for (var l = 0; l < Context.Viewports.Depth; l++)
                {
                    if (l == Context.Layer) continue;
                    DrawTexture(Context.Viewports.Geos[l].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 120 });
                }
                
                DrawRectangle(posX: 0, posY: 0, width: level.Width * 20, height: level.Height * 20, Color.Red with { A = 40 });

                DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 210 });
            break;
        }
        EndTextureMode();
    }

    private void PlaceOne(int mx, int my, int mz, Geo geo)
    {
        if (Context.SelectedLevel is not { } level || !level.Geos.IsInBounds(mx, my, mz)) return;

        level.Geos[mx, my, mz] = geo;

        BeginTextureMode(Context.Viewports.Geos[mz]);
        var sourceRect = GeoAtlas.GetRect(atlas.GetIndex(geo));

        BeginBlendMode(BlendMode.Custom);
        Rlgl.SetBlendFactors(glSrcFactor: 1, glDstFactor: 0, glEquation: 1);
        DrawTextureRec(
            texture: atlas.Texture, 
            source: sourceRect, 
            position: new Vector2(mx * 20, my *20), 
            tint: Color.White
        );
        EndBlendMode();
        EndTextureMode();

        redrawMain = true;
    }

    private void PlaceBrush(Geo geo)
    {
        if (brushRadius == 0)
        {
            PlaceOne(cursor.MX, cursor.MY, Context.Layer, geo);
            return;
        }

        if (Context.SelectedLevel is not { } level) return;

        var sourceRect = GeoAtlas.GetRect(atlas.GetIndex(geo));

        BeginTextureMode(Context.Viewports.Geos[Context.Layer]);

        for (int x = cursor.MX - brushRadius; x < cursor.MX + brushRadius + 1; x++)
        {
            if (x < 0 || x >= level.Geos.Width) continue;

            for (int y = cursor.MY - brushRadius; y < cursor.MY + brushRadius + 1; y++)
            {
                if (y < 0 || y >= level.Geos.Height) continue;
                if (!IsInBrush(x, y)) continue;
                if (level.Geos[x, y, Context.Layer] == geo) continue;

                level.Geos[x, y, Context.Layer] = geo;

                BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(glSrcFactor: 1, glDstFactor: 0, glEquation: 1);
                DrawTextureRec(
                    texture: atlas.Texture, 
                    source: sourceRect, 
                    position: new Vector2(x * 20, y *20), 
                    tint: Color.White
                );
                EndBlendMode();
            }
        }

        EndTextureMode();

        redrawMain = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnAreaSelected(Rectangle rect, bool isErasing)
    {
        for (var y = (int)rect.Y; y < rect.Y + rect.Height; y++)
        for (var x = (int)rect.X; x < rect.X + rect.Width; x++)
        {
            PlaceOne(x, y, Context.Layer, isErasing ? Geo.Air : selectedGeo);
        }
    }

    public override void OnViewSelected()
    {
        redrawMain = true;
    }

    public override void OnLevelSelected(Level level)
    {
        Array.Fill(redrawGeos, true);
        redrawMain = true;
    }

    public override void Process()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();
            cursor.ProcessSelection();

            if (IsKeyPressed(KeyboardKey.L))
            {
                Context.Layer = ++Context.Layer % Context.SelectedLevel!.Depth;

                if (Context.Config.GeoColoring == GeometryLayerColoring.Gray) redrawMain = true;
            }

            // Change brush size/corner
            var wheel = GetMouseWheelMove();
            if (IsKeyDown(KeyboardKey.LeftAlt) && wheel != 0)
            {
                if (IsKeyDown(KeyboardKey.LeftControl))
                    brushCorner = (int)Math.Clamp(brushCorner + wheel, 0, 10);                  
                else
                    brushRadius = (int)Math.Clamp(brushRadius + wheel, 0, 10);
            }
            //

            if (cursor.IsInMatrix)
            {
                if (!cursor.IsSelecting)
                {
                    if (IsMouseButtonDown(MouseButton.Left))
                    {
                        var toplace = selectedGeo;
                        var cell = Context.SelectedLevel?.Geos[cursor.MX, cursor.MY, Context.Layer];

                        switch ((cell, toplace))
                        {
                            case (Geo.VerticalPole, Geo.HorizontalPole):
                            case (Geo.HorizontalPole, Geo.VerticalPole):
                            toplace = Geo.CrossPole;
                            break;
                        }

                        PlaceBrush(toplace);
                    }
                    else if (IsMouseButtonDown(MouseButton.Right))
                    {
                        PlaceBrush(Geo.Air);
                    }
                    else if (IsKeyPressed(KeyboardKey.C) && IsKeyDown(KeyboardKey.LeftControl))
                    {
                        memory = new Geo[brushRadius*2 + 1, brushRadius*2 + 1];

                        for (var x = 0; x < memory.GetLength(dimension: 0); x++)
                        {
                            var mx = x + cursor.MX + brushRadius + 1;
                            if (mx < 0 || mx >= level.Width) continue;

                            for (var y = 0; y < memory.GetLength(dimension: 1); y++)
                            {
                                var my = y + cursor.MY + brushRadius + 1;
                                if (my < 0 || my >= level.Height) continue;

                                memory[x, y] = level.Geos[mx, my, Context.Layer];
                            }
                        }
                    }
                    else if (IsKeyPressed(KeyboardKey.V) && IsKeyDown(KeyboardKey.LeftControl))
                    {
                        isPasting = !isPasting;
                    }
                }
            }
        }

        //

        if (IsKeyPressed(KeyboardKey.W))
        {
            selectedGeoIndex.y -= 1;
            if (selectedGeoIndex.y < 0) selectedGeoIndex.y = 6;
            selectedGeo = GeoMenuIndex(selectedGeoIndex.x, selectedGeoIndex.y);
            DrawGeosMenu();
        }
        else if (IsKeyPressed(KeyboardKey.S))
        {
            selectedGeoIndex.y += 1;
            if (selectedGeoIndex.y > 6) selectedGeoIndex.y = 0;
            selectedGeo = GeoMenuIndex(selectedGeoIndex.x, selectedGeoIndex.y);
            DrawGeosMenu();
        }

        if (IsKeyPressed(KeyboardKey.A))
        {
            selectedGeoIndex.x -= 1;
            if (selectedGeoIndex.x < 0) selectedGeoIndex.x = 1;
            selectedGeo = GeoMenuIndex(selectedGeoIndex.x, selectedGeoIndex.y);
            DrawGeosMenu();
        }
        else if (IsKeyPressed(KeyboardKey.D))
        {
            selectedGeoIndex.x += 1;
            if (selectedGeoIndex.x > 1) selectedGeoIndex.x = 0;
            selectedGeo = GeoMenuIndex(selectedGeoIndex.x, selectedGeoIndex.y);
            DrawGeosMenu();
        }
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is null) return;

        var level = Context.SelectedLevel;

        for (var l = 0; l < Context.Viewports.Depth; l++)
        {
            if (redrawGeos[l]) {
                DrawGeosViewport(l);
                redrawMain = true;
            }
        }
        Array.Fill(redrawGeos, false);

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

        cursor.DrawGrid();
        DrawBrush();
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin(name: "Options##GeosOptions"))
        {
            ImGui.SeparatorText(label: "View");
            foreach (var mode in Enum.GetValues<GeometryLayerColoring>())
            {
                if (ImGui.RadioButton(label: $"{mode}", active: mode == Context.Config.GeoColoring))
                {
                    Context.Config.GeoColoring = mode;
                    redrawMain = true;
                }
            }

        }
        ImGui.End();

        if (ImGui.Begin(name: "Geos Menu"))
        {
            rlImGui_cs.rlImGui.ImageRenderTextureFit(geosMenu, center: false);
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Layer", Context.Layer, Color.Magenta);
        printer.PrintlnLabel("Selected", selectedGeo, Color.SkyBlue);

        if (Context.SelectedLevel is { } level)
        {
            if (cursor.IsInMatrix)
            {
                printer.PrintlnLabel(
                    "Hovered", 
                    level.Geos[cursor.MX, cursor.MY, Context.Layer],
                    Color.Gold
                ); 
            }
        }

        printer.PrintlnLabel("Brush Size", brushRadius, Color.Magenta);
        printer.PrintlnLabel("Brush Corner", brushCorner, Color.Magenta);
    }
}