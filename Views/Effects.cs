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

public class Effects : BaseView
{
    private readonly Cursor cursor;

    private int brushSize;

    private EffectDef? selectedEffectDef;
    private Effect? selectedEffect;

    private bool redrawMain;
    private bool redrawEffects;

    private bool brushLock;
    private Vector2 prevMXPos;

    private int tintOpacity;

    public Effects(Context context) : base(context)
    {
        cursor = new Cursor(context);

        tintOpacity = 80;
    }

    private void Place(int mx, int my, int radius, float power)
    {
        if (selectedEffect is null) return;

        for (int x = mx - radius; x < mx + radius + 1; x++)
        {
            if (x < 0 || x >= selectedEffect.Matrix.Width) continue;

            for (int y = my - radius; y < my + radius + 1; y++)
            {
                if (y < 0 || y >= selectedEffect.Matrix.Height) continue;

                if (CheckCollisionPointCircle((new Vector2(x, y) * 20) + (Vector2.One * 0.5f), (cursor.MXPos*20) + (Vector2.One * 0.5f), (brushSize + 1) * 20))
                {
                    selectedEffect.Matrix[x, y, 0] = Math.Clamp(
                        selectedEffect.Matrix[x, y, 0] + (power * (1 - Raymath.Vector2Distance(cursor.MXPos, new Vector2(x, y))/radius)), 
                        0, 
                        1f
                    );
                }
            }
        }
    }

    public override void OnLevelSelected(Level level)
    {
        redrawEffects = true;
        redrawMain = true;
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

            var wheel = GetMouseWheelMove();

            if (IsKeyDown(KeyboardKey.LeftAlt) && wheel != 0)
            {
                brushSize = (int)Math.Clamp(brushSize + wheel, 0, 10);
            }

            if (IsKeyPressed(KeyboardKey.W))
            {
                var index = selectedEffect is null ? 0 : level.Effects.IndexOf(selectedEffect);

                if (index != -1)
                {
                    index--;
                    if (index == -1) selectedEffect = level.Effects[^1];
                    else selectedEffect = level.Effects[index];
                }
            }
            else if (IsKeyPressed(KeyboardKey.S))
            {
                var index = selectedEffect is null ? 0 : level.Effects.IndexOf(selectedEffect);

                if (index != -1)
                {
                    index++;
                    if (index == level.Effects.Count) selectedEffect = level.Effects[0];
                    else selectedEffect = level.Effects[index];
                }
            }

            if (IsKeyPressed(KeyboardKey.A))
            {
                
            }
            else if (IsKeyPressed(KeyboardKey.D))
            {
                
            }

            if (IsKeyPressed(KeyboardKey.X))
            {
                if (selectedEffect is not null)
                {
                    var index = level.Effects.IndexOf(selectedEffect);

                    level.Effects.Remove(selectedEffect);
                    if (index < level.Effects.Count)
                    {
                        selectedEffect = level.Effects[index];
                    }
                    else
                    {
                        selectedEffect = null;
                    }
                }
            }

            var power = 0.1f;

            if (IsKeyDown(KeyboardKey.LeftShift)) power *= 2;

            if (cursor.IsInMatrix)
            {
                if (selectedEffect is not null)
                {
                    if (IsMouseButtonDown(MouseButton.Left) && (!brushLock || prevMXPos != cursor.MXPos))
                    {
                        brushLock = true;

                        Place(cursor.MX, cursor.MY, brushSize + 1, power);
                        redrawEffects = true;

                        prevMXPos = cursor.MXPos;
                    }
                    else if (IsMouseButtonDown(MouseButton.Right) && (!brushLock || prevMXPos != cursor.MXPos))
                    {
                        brushLock = true;

                        Place(cursor.MX, cursor.MY, brushSize + 1, -power);
                        redrawEffects = true;

                        prevMXPos = cursor.MXPos;
                    }

                    if (IsMouseButtonReleased(MouseButton.Left) || IsMouseButtonReleased(MouseButton.Right))
                    {
                        brushLock = false;
                    }
                }
            }
        }
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (redrawEffects)
        {
            var vp = Context.Viewports.Effect;

            vp.Clear();

            if (selectedEffect is not null)
            {    
                BeginTextureMode(vp);

                for (var y = 0; y < level.Height; y++)
                {
                    if (y < 0 || y >= selectedEffect.Matrix.Height) continue;
                    
                    for (var x = 0; x < level.Width; x++)
                    {
                        if (x < 0 || x >= selectedEffect.Matrix.Width) continue;
                        DrawRectangle(x * 20, y * 20, 20, 20, Color.Green with { A = (byte)(selectedEffect.Matrix[x, y, 0] * 240) });
                    }
                }

                EndTextureMode();
            }


            redrawEffects = false;
            redrawMain = true;
        }

        if (redrawMain)
        {
            BeginTextureMode(Context.Viewports.Main);
            ClearBackground(new Color(0, 0, 0, 0));
            for (int l = Context.Viewports.Depth - 1; l > -1; --l)
            {
                if (l == Context.Layer) continue;
                DrawTexture(Context.Viewports.Geos[l].Raw.Texture, 0, 0, Color.Black with { A = 120 });
                DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, 0, 0, Color.White with { A = 120 });
            }

            DrawRectangle(0, 0, level.Width * 20, level.Height * 20, Color.Red with { A = 40 });

            DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, 0, 0, Color.Black with { A = 210 });
            DrawTexture(Context.Viewports.Tiles[Context.Layer].Raw.Texture, 0, 0, Color.White with { A = 210 });

            DrawRectangle(0, 0, Context.Viewports.Main.Width, Context.Viewports.Main.Height, Color.Magenta with { A = (byte)tintOpacity });

            DrawTexture(Context.Viewports.Effect.Texture, 0, 0, Color.White);

            EndTextureMode();

            Context.Viewer.Props.DrawPlacedProps();
            
            redrawMain = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, 0, 0, Color.White);


        cursor.DrawCursor();

        for (var x = cursor.MX - brushSize; x < cursor.MX + brushSize + 1; x++)
        {
            for (var y = cursor.MY - brushSize; y < cursor.MY + brushSize + 1; y++)
            {
                if (CheckCollisionPointCircle((new Vector2(x, y) * 20) + (Vector2.One * 0.5f), (cursor.MXPos*20) + (Vector2.One * 0.5f), (brushSize + 1) * 20))
                {
                    DrawRectangle(x * 20, y * 20, 20, 20, Color.White with { A = 30 });
                }
            }
        }
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (Context.SelectedLevel is not { } level) return;

        if (ImGui.Begin("Menu##EffectMenu"))
        {
            if (selectedEffectDef is null) ImGui.BeginDisabled();
            if (ImGui.Button("Add", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                level.Effects.Add(new Effect(selectedEffectDef!, level.Width, level.Height));
            }
            if (selectedEffectDef is null) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##Effects", ImGui.GetContentRegionAvail()))
            {
                foreach (var category in Context.Effects.Categories)
                {
                    ImGui.SeparatorText(category);

                    foreach (var effect in Context.Effects.CategoryEffects[category])
                    {
                        if (ImGui.Selectable($"{effect.Name}##{effect.ID}", effect == selectedEffectDef))
                        {
                            selectedEffectDef = effect;
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin("Effects##EffectList"))
        {
            if (selectedEffect is null) ImGui.BeginDisabled();
            if (ImGui.Button("Remove", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                var index = level.Effects.IndexOf(selectedEffect!);

                level.Effects.Remove(selectedEffect!);
                if (index < level.Effects.Count)
                {
                    selectedEffect = level.Effects[index];
                }
                else
                {
                    selectedEffect = null;
                }
            }
            if (selectedEffect is null) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##Effects", ImGui.GetContentRegionAvail()))
            {
                for (var e = 0; e < level.Effects.Count; e++)
                {
                    var effect = level.Effects[e];

                    if (ImGui.Selectable($"{e}. {effect.Def.Name}", effect == selectedEffect))
                    {
                        selectedEffect = effect;
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin("Settings##EffectEditorSettings"))
        {
            if (ImGui.SliderInt("Tint Opacity", ref tintOpacity, 0, 250))
            {
                redrawMain = true;
            }
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Brush Size", brushSize, Color.Magenta);
    }
}