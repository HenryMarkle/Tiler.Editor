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
    private readonly Cursor cursor;
    private readonly List<Texture> brushes;
    private int selectedBrushIndex;
    private Texture? selectedBrush;
    private Vector2 brushSize;
    private float brushRotation;
    private bool isRotatingBrushWithCursor;
    private Vector2 pinnedBrushPos;
    private readonly Shader eraseShader;

    public Light(Context context) : base(context)
    {
        cursor = new Cursor(context);

        brushes = !Directory.Exists(context.Dirs.Light) ? [] : Directory
            .GetFiles(context.Dirs.Light)
            .Where(f => f.EndsWith(".png"))
            .Select(f => new Texture(LoadTexture(f)))
            .ToList();

        brushSize = Vector2.One * 200;

        if (brushes.Count > 0)
        {
            selectedBrush = brushes[0];
            selectedBrushIndex = 0;
        }
        else
        {
            selectedBrushIndex = -1;
        }

        eraseShader = LoadShaderFromMemory(
            vsCode: null, 
            fsCode: @"
            #version 330
            
            uniform sampler2D texture0;

            in vec2 fragTexCoord;
            in vec4 fragColor;

            out vec4 finalColor;

            void main() {
                vec4 pixel = texture(texture0, fragTexCoord);
                
                if (pixel == vec4(1, 1, 1, 1)) {
                    finalColor = vec4(0, 0, 0, 0);
                } else {
                    discard;
                }
            }
            "
        );
    }

    ~Light()
    {
        UnloadShader(eraseShader);
    }
    

    public override void OnLevelSelected(Level level)
    {
        if (Context.SelectedLevel is not null)
            Context.SelectedLevel.Lightmap = new Managed.Image(
                LoadImageFromTexture(Context.Viewports.Lightmap.Raw.Texture)
            );

        var lightmap = new Texture(LoadTextureFromImage(level.Lightmap));

        BeginTextureMode(Context.Viewports.Lightmap);
        ClearBackground(new Color(0, 0, 0, 0));
        DrawTexture(lightmap, 0, 0, Color.White);
        EndTextureMode();
    }

    public override void OnViewSelected()
    {
        base.OnViewSelected();
    }


    public override void Process()
    {
        if (!cursor.IsInWindow)
        {
            cursor.ProcessCursor();
        }

        if (brushes.Count == 0) return;

        if (IsKeyPressed(KeyboardKey.R))
        {
            selectedBrushIndex--;

            if (selectedBrushIndex < 0) selectedBrushIndex = brushes.Count - 1;

            selectedBrush = brushes[selectedBrushIndex];
        }
        else if (IsKeyPressed(KeyboardKey.F))
        {
            selectedBrushIndex = ++selectedBrushIndex % brushes.Count;

            selectedBrush = brushes[selectedBrushIndex];
        }

        // Deform

        var multiplier = 1;

        if (IsKeyDown(KeyboardKey.LeftShift)) multiplier = 3;

        if (IsKeyDown(KeyboardKey.W))
        {
            brushSize.Y += multiplier;
        }
        else if (IsKeyDown(KeyboardKey.S))
        {
            brushSize.Y -= multiplier;
        }

        if (IsKeyDown(KeyboardKey.D))
        {
            brushSize.X += multiplier;
        }
        else if (IsKeyDown(KeyboardKey.A))
        {
            brushSize.X -= multiplier;
        }

        if (IsKeyDown(KeyboardKey.E))
        {
            brushRotation += multiplier;
        }
        else if (IsKeyDown(KeyboardKey.Q))
        {
            brushRotation -= multiplier;
        }

        if (isRotatingBrushWithCursor)
        {
            brushRotation = MathF.Atan2(
                y: cursor.Y - pinnedBrushPos.Y,
                x: cursor.X - pinnedBrushPos.X
            );

            brushRotation = float.RadiansToDegrees(brushRotation) + 90;

            if (IsKeyReleased(KeyboardKey.LeftControl))
            {
                isRotatingBrushWithCursor = false;
            }
        }
        else
        {
            if (IsKeyDown(KeyboardKey.LeftControl))
            {
                pinnedBrushPos = new Vector2(cursor.X, cursor.Y);
                isRotatingBrushWithCursor = true;
            }
        }
    }

    public override void Draw()
    {
        if (!cursor.IsInWindow)
        {
            if (IsMouseButtonDown(MouseButton.Left))
            {
                if (selectedBrush is not null)
                {
                    RlUtils.DrawTextureRT(
                        rt:          Context.Viewports.Lightmap, 
                        texture:     selectedBrush, 
                        source:      new Rectangle(0, 0, selectedBrush.Width, selectedBrush.Height),
                        destination: new Rectangle(
                            cursor.X + Viewports.LightmapMargin, 
                            cursor.Y + Viewports.LightmapMargin, 
                            brushSize
                        ),
                        origin:      brushSize/2,
                        rotation:    brushRotation,
                        tint:        Color.White
                    );

                }
            }
            else if (IsMouseButtonDown(MouseButton.Right))
            {
                if (selectedBrush is not null) 
                {
                    Rlgl.SetBlendMode(BlendMode.Custom);

                    Rlgl.SetBlendFactors(1, 0, 1);

                    BeginShaderMode(eraseShader);

                    SetShaderValueTexture(
                        shader:   eraseShader,
                        locIndex: GetShaderLocation(eraseShader, "texture0"),
                        texture:  selectedBrush
                    );

                    RlUtils.DrawTextureRT(
                        rt:          Context.Viewports.Lightmap, 
                        texture:     selectedBrush, 
                        source:      new Rectangle(0, 0, selectedBrush.Width, selectedBrush.Height),
                        destination: new Rectangle(
                            cursor.X + Viewports.LightmapMargin, 
                            cursor.Y + Viewports.LightmapMargin, 
                            brushSize
                        ),
                        origin:      brushSize/2,
                        rotation:    brushRotation,
                        tint:        Color.White
                    );

                    EndShaderMode();

                    Rlgl.SetBlendMode(BlendMode.Alpha);
                }
            }
        }

        BeginMode2D(Context.Camera);
        DrawTexture(
            texture: Context.Viewports.Main.Raw.Texture, 
            posX:    0, 
            posY:    0, 
            tint:    Color.White
        );
        DrawRectangle(
            posX:   -Viewports.LightmapMargin, 
            posY:   -Viewports.LightmapMargin, 
            width:  Context.Viewports.Lightmap.Width,
            height: Context.Viewports.Lightmap.Height,
            color:  Color.White with { A = 80 }
        );
        DrawTexture(
            texture: Context.Viewports.Lightmap.Raw.Texture, 
            posX:    -Viewports.LightmapMargin, 
            posY:    -Viewports.LightmapMargin, 
            tint:    Color.Black
        );

        if (selectedBrush is not null)
            DrawTexturePro(
                texture:  selectedBrush,
                source:   new Rectangle(0, 0, selectedBrush.Width, selectedBrush.Height),
                dest:     isRotatingBrushWithCursor 
                            ? new Rectangle(pinnedBrushPos, brushSize) 
                            : new Rectangle(cursor.X, cursor.Y, brushSize),
                origin:   brushSize/2,
                rotation: brushRotation, 
                tint:     new Color(200, 0, 0, 90)
            );
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin("Brushes"))
        {
            if (ImGui.BeginListBox("##Brushes", ImGui.GetContentRegionAvail()))
            {
                var space = ImGui.GetContentRegionAvail();

                foreach (var brush in brushes)
                {
                    var pos = ImGui.GetCursorScreenPos();
                    
                    if (brush == selectedBrush)
                    {
                        var drawl = ImGui.GetWindowDrawList();

                        drawl.AddRectFilled(
                            pos,
                            pos + Vector2.One * space.X,
                            ImGui.ColorConvertFloat4ToU32(
                                new Vector4(0, 0.87f, 0.1f, 1)
                            )
                        );

                        ImGui.SetCursorScreenPos(pos);
                    }

                    if (ImGui.IsMouseHoveringRect(pos, pos + Vector2.One * space.X))
                    {
                        var drawl = ImGui.GetWindowDrawList();

                        drawl.AddRectFilled(
                            pos,
                            pos + Vector2.One * space.X,
                            ImGui.ColorConvertFloat4ToU32(
                                new Vector4(0, 0.87f, 0.8f, 0.9f)
                            )
                        );

                        ImGui.SetCursorScreenPos(pos);

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            selectedBrush = brush;
                            selectedBrushIndex = brushes.IndexOf(selectedBrush);
                        }
                    }

                    rlImGui_cs.rlImGui.ImageSize(brush, new(space.X, space.X));
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

        printer.PrintlnLabel("Size", brushSize, Color.SkyBlue);
        printer.PrintlnLabel("Rotation", brushRotation, Color.SkyBlue);
    }
}