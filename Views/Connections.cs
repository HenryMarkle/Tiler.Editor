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
using Tiler.Editor.Views.Components;
using static Raylib_cs.Raylib;

public class Connections : BaseView
{
    private readonly Cursor cursor;
    private readonly ConnectionAtlas atlas;
    private ConnectionType selectedConnectionType;

    private bool redrawMain;
    private bool redrawConnections;

    private bool drawGrid;

    public Connections(Context context) : base(context)
    {
        cursor = new Cursor(context);

        selectedConnectionType = ConnectionType.Path;

        atlas = new ConnectionAtlas(new Texture(LoadTexture(context.Dirs.Files.ConnectionsAtlas)));
    }

    public override void OnLevelSelected(Level level)
    {
        redrawConnections = true;
    }

    public override void OnViewSelected()
    {
        redrawMain = true;
    }

    private void DrawConnectionsViewport()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Connections);
        ClearBackground(new Color(0, 0, 0, 0));
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var conn = level.Connections[x, y, 0];
                
                if (conn is ConnectionType.None) continue;

                var source = ConnectionAtlas.GetRect(atlas.GetIndex(conn));
            
                DrawTexturePro(
                    texture:  atlas.Texture,
                    source,
                    dest:     new Rectangle(x * 20, y * 20, 20, 20),
                    origin:   Vector2.Zero,
                    rotation: 0,
                    tint:     Color.White
                );
            }
        }
        EndTextureMode();
    }

    private void DrawMainViewport()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Main);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int l = Context.Viewports.Depth - 1; l > 0; --l)
        {
            DrawTexture(Context.Viewports.Geos[l].Texture, 0, 0, Color.Black with { A = 120 });
            DrawTexture(Context.Viewports.Tiles[l].Texture, 0, 0, Color.White with { A = 120 });
        }
        
        DrawRectangle(0, 0, level.Width * 20, level.Height * 20, Color.Red with { A = 40 });

        DrawTexture(Context.Viewports.Geos[0].Texture, 0, 0, Color.Black with { A = 210 });
        DrawTexture(Context.Viewports.Tiles[0].Texture, 0, 0, Color.White with { A = 210 });
        DrawTexture(Context.Viewports.Connections.Texture, 0, 0, Color.White);
        EndTextureMode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PlaceOne(int mx, int my, ConnectionType connection)
    {
        if (Context.SelectedLevel is not { } level || !level.Connections.IsInBounds(mx, my, 0)) return;

        level.Connections[mx, my, 0] = connection;

        BeginTextureMode(Context.Viewports.Connections);
        
        // Clear area
        BeginBlendMode(BlendMode.Custom);
        Rlgl.SetBlendMode(BlendMode.Custom);
        Rlgl.SetBlendFactors(1, 0, 1);
        DrawRectangle(
            posX:   mx * 20, 
            posY:   my * 20, 
            width:  20, 
            height: 20,
            color:  new Color(0, 0, 0, 0)
        );
        EndBlendMode();

        if (connection is not ConnectionType.None)
        {    
            DrawTextureRec(
                texture:  atlas.Texture, 
                source:   ConnectionAtlas.GetRect(atlas.GetIndex(connection)), 
                position: new Vector2(mx * 20, my *20), 
                tint:     Color.White
            );
        }

        EndTextureMode();

        redrawMain = true;
    }

    public override void Process()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();

            if (cursor.IsInMatrix)
            {
                if (IsMouseButtonDown(MouseButton.Left))
                {
                    var conn = level.Connections[cursor.MX, cursor.MY, 0];

                    if (conn != selectedConnectionType)
                    {
                        PlaceOne(cursor.MX, cursor.MY, selectedConnectionType);
                    }
                }
                else if (IsMouseButtonDown(MouseButton.Right))
                {
                    var conn = level.Connections[cursor.MX, cursor.MY, 0];

                    if (conn != ConnectionType.None)
                    {
                        PlaceOne(cursor.MX, cursor.MY, ConnectionType.None);
                    }
                }
            }
        }

        if (IsKeyPressed(KeyboardKey.G)) 
            drawGrid = !drawGrid;

        if (IsKeyPressed(KeyboardKey.A))
        {
            selectedConnectionType = (ConnectionType) Math.Clamp((int)selectedConnectionType - 1, 1, 5);
        }
        else if (IsKeyPressed(KeyboardKey.D))
        {
            selectedConnectionType = (ConnectionType) Math.Clamp((int)selectedConnectionType + 1, 1, 5);
        }
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (redrawConnections)
        {
            DrawConnectionsViewport();
            redrawConnections = false;
            redrawMain = true;
        }

        if (redrawMain)
        {
            DrawMainViewport();
            redrawMain = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, 0, 0, Color.White);

        if (drawGrid) cursor.DrawGrid();
        cursor.DrawCursor();

        DrawTexturePro(
            texture:  atlas.Texture,
            source:   ConnectionAtlas.GetRect(atlas.GetIndex(selectedConnectionType)),
            dest:     new Rectangle(cursor.MXPos * 20 + Vector2.One * 20, 40, 40),
            origin:   Vector2.Zero,
            rotation: 0,
            tint:     Color.White with { A = 80 }
        );
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin("Connections"))
        {
            var space = ImGui.GetContentRegionAvail();
            var imageSize = new Vector2(atlas.Texture.Width, atlas.Texture.Height);
            
            float aspectRatio = imageSize.X / imageSize.Y;
            
            Vector2 displaySize;

            if (space.X / space.Y > aspectRatio)
                displaySize = new Vector2(space.Y * aspectRatio, space.Y);
            else
                displaySize = new Vector2(space.X, space.X / aspectRatio);

            var drawl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();

            var colRectSize = MathF.Min(displaySize.X, displaySize.Y);

            drawl.AddRectFilled(
                p_min: new Vector2(pos.X + colRectSize * ((int)selectedConnectionType - 1), pos.Y),
                p_max: new Vector2(pos.X + colRectSize * ((int)selectedConnectionType - 1) + colRectSize, pos.Y + colRectSize),
                col:   ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.5f, 0.9f, 0.78f))
            );

            ImGui.SetCursorScreenPos(pos);
            
            rlImGui_cs.rlImGui.ImageSize(atlas.Texture, displaySize);
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintLabel("Selected", selectedConnectionType, Color.Gold);
    }
}