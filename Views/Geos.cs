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
        redrawMain = true;
        redrawGeos = new bool[context.Viewports.Depth];
        Array.Fill(redrawGeos, true);

        atlas = new GeoAtlas(
            new Texture(
                LoadTexture(Path.Combine(Context.Dirs.Textures, "geometry", "atlas.png"))));

        cursor = new Cursor(context);

        cursor.AreaSelected += OnAreaSelected;
        geosMenu = new(LoadRenderTexture(40, 140));
        selectedGeoIndex = (0, 0);
        selectedGeo = Geo.Solid;
        
        DrawGeosMenu();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rectangle GeoMenuRect(Geo g) => g switch
    {
        Geo.Solid => new(0, 0, 20, 20),
        Geo.Air => new(20, 0, 20, 20),
        Geo.Slab => new(20, 20, 20, 20),
        Geo.Wall => new(0, 20, 20, 20),
        Geo.Platform => new(20, 40, 20, 20),
        Geo.Glass => new(0, 40, 20, 20),
        Geo.Exit => new(20, 60, 20, 20),
        Geo.VerticalPole => new(0, 60, 20, 20),
        Geo.CrossPole => new(20, 80, 20, 20),
        Geo.HorizontalPole => new(0, 80, 20, 20),
        Geo.SlopeNW => new(0, 100, 20, 20),
        Geo.SlopeNE => new(20, 100, 20, 20),
        Geo.SlopeSW => new(0, 120, 20, 20),
        Geo.SlopeSE => new(20, 120, 20, 20),
        _ => new(0, 0, 20, 20),
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

        for (int x = 0; x < 14; x++)
            DrawTexturePro(
                atlas.Texture,
                GeoAtlas.GetRect(atlas.GetIndex((Geo)x)),
                GeoMenuRect((Geo)x), 
                Vector2.Zero, 
                0, 
                Color.White
            );

        DrawRectangleLinesEx(
            new Rectangle(20 * selectedGeoIndex.x, 20 * selectedGeoIndex.y, 20, 20), 1.0f, Color.Red);

        EndTextureMode();
    }

    private void DrawGeosViewport(int layer)
    {
        if (Context.SelectedLevel is null) return;

        var level = Context.SelectedLevel;

        var vp = Context.Viewports.Geos[layer];

        BeginTextureMode(vp);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                var geo = level.Geos[x, y, layer];

                var sourceRect = GeoAtlas.GetRect(atlas.GetIndex(geo));

                BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(1, 0, 1);
                DrawTextureRec(
                    atlas.Texture, 
                    sourceRect, 
                    new Vector2(x * 20, y *20), 
                    Color.White
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
                DrawTexture(Context.Viewports.Geos[0].Raw.Texture, 0, 0, new Color(0, 255, 255, 255));
                DrawTexture(Context.Viewports.Geos[1].Raw.Texture, 0, 0, new Color(255, 0, 255, 80));
                DrawTexture(Context.Viewports.Geos[2].Raw.Texture, 0, 0, new Color(255, 0, 0, 80));
            break;

            case GeometryLayerColoring.RGB:
                ClearBackground(Color.Gray);
                DrawTexture(Context.Viewports.Geos[0].Raw.Texture, 0, 0, new Color(0, 0, 0, 255));
                DrawTexture(Context.Viewports.Geos[1].Raw.Texture, 0, 0, new Color(0, 255, 0, 80));
                DrawTexture(Context.Viewports.Geos[2].Raw.Texture, 0, 0, new Color(255, 0, 0, 80));
            break;

            case GeometryLayerColoring.Gray:
                ClearBackground(new Color(0, 0, 0, 0));
                for (int l = 0; l < Context.Viewports.Depth; l++)
                {
                    if (l == Context.Layer) continue;
                    DrawTexture(Context.Viewports.Geos[l].Raw.Texture, 0, 0, Color.Black with { A = 120 });
                }
                
                DrawRectangle(0, 0, level.Width * 20, level.Height * 20, Color.Red with { A = 40 });

                DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, 0, 0, Color.Black with { A = 210 });
            break;
        }
        EndTextureMode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PlaceOne(int mx, int my, int mz, Geo geo)
    {
        if (Context.SelectedLevel is not { } level || !level.Geos.IsInBounds(mx, my, mz)) return;

        level.Geos[mx, my, mz] = geo;

        BeginTextureMode(Context.Viewports.Geos[mz]);
        var sourceRect = GeoAtlas.GetRect(atlas.GetIndex(geo));

        BeginBlendMode(BlendMode.Custom);
        Rlgl.SetBlendFactors(1, 0, 1);
        DrawTextureRec(
            atlas.Texture, 
            sourceRect, 
            new Vector2(mx * 20, my *20), 
            Color.White
        );
        EndBlendMode();
        EndTextureMode();

        redrawMain = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnAreaSelected(Rectangle rect, bool isErasing)
    {
        for (int y = (int)rect.Y; y < rect.Y + rect.Height; y++)
        for (int x = (int)rect.X; x < rect.X + rect.Width; x++)
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
        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();
            cursor.ProcessSelection();

            if (IsKeyPressed(KeyboardKey.L))
            {
                Context.Layer = ++Context.Layer % 3;

                if (Context.Config.GeoColoring == GeometryLayerColoring.Gray) redrawMain = true;
            }

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

                        if (Context.SelectedLevel?.Geos[cursor.MX, cursor.MY, Context.Layer] != toplace)
                            PlaceOne(cursor.MX, cursor.MY, Context.Layer, toplace);
                    }
                    else if (IsMouseButtonDown(MouseButton.Right))
                    {
                        if (Context.SelectedLevel?.Geos[cursor.MX, cursor.MY, Context.Layer] != Geo.Air)
                            PlaceOne(cursor.MX, cursor.MY, Context.Layer, Geo.Air);
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

        for (int l = 0; l < Context.Viewports.Depth; l++)
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
        DrawTexture(Context.Viewports.Main.Raw.Texture, 0, 0, Color.White);
        DrawRectangleLinesEx(new Rectangle(0, 0, level.Width *20f, level.Height * 20f), 4, Color.Black);
        DrawRectangleLinesEx(new Rectangle(0, 0, level.Width *20f, level.Height * 20f), 2, Color.White);

        cursor.DrawGrid();
        cursor.DrawCursor();
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin("Options##GeosOptions"))
        {
            ImGui.SeparatorText("View");
            foreach (var mode in Enum.GetValues<GeometryLayerColoring>())
            {
                if (ImGui.RadioButton($"{mode}", mode == Context.Config.GeoColoring))
                {
                    Context.Config.GeoColoring = mode;
                    redrawMain = true;
                }
            }

        }
        ImGui.End();

        if (ImGui.Begin("Geos Menu"))
        {
            rlImGui_cs.rlImGui.ImageRenderTextureFit(geosMenu, false);
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Layer", Context.Layer, Color.Magenta);

        if (Context.SelectedLevel is { } level)
        {
            if (cursor.IsInMatrix)
            {
                printer.PrintlnLabel(
                    "Geo", 
                    level.Geos[cursor.MX, cursor.MY, Context.Layer],
                    Color.Gold
                ); 
            }
        }

    }
}