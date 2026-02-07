namespace Tiler.Editor.Views;

using System;
using System.Numerics;

using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Tiler.Editor.Views.Components;

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

        for (var x = mx - radius; x < mx + radius + 1; x++)
        {
            if (x < 0 || x >= selectedEffect.Matrix.Width) continue;

            for (var y = my - radius; y < my + radius + 1; y++)
            {
                if (y < 0 || y >= selectedEffect.Matrix.Height) continue;

                if (CheckCollisionPointCircle(
                        point: (new Vector2(x, y) * 20) + (Vector2.One * 0.5f), 
                        center: (cursor.MXPos*20) + (Vector2.One * 0.5f), 
                        radius: (brushSize + 1) * 20)
                    )
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

                        Place(cursor.MX, cursor.MY, radius: brushSize + 1, power);
                        redrawEffects = true;

                        prevMXPos = cursor.MXPos;
                    }
                    else if (IsMouseButtonDown(MouseButton.Right) && (!brushLock || prevMXPos != cursor.MXPos))
                    {
                        brushLock = true;

                        Place(cursor.MX, cursor.MY, radius: brushSize + 1, -power);
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
                        DrawRectangle(
                            posX: x * 20, 
                            posY: y * 20, 
                            width: 20, 
                            height: 20, 
                            Color.Green with { A = (byte)(selectedEffect.Matrix[x, y, 0] * 240) }
                            );
                    }
                }

                EndTextureMode();
            }


            redrawEffects = false;
            redrawMain = true;
        }

        if (redrawMain)
        {
            BeginTextureMode(Context.Viewports.Props);
            ClearBackground(new Color(0, 0, 0, 0));
            Context.Viewer.Props.DrawPlacedProps();
            EndTextureMode();

            BeginTextureMode(Context.Viewports.Main);
            ClearBackground(new Color(0, 0, 0, 0));
            for (int l = Context.Viewports.Depth - 1; l > -1; --l)
            {
                if (l == Context.Layer) continue;
                DrawTexture(Context.Viewports.Geos[l].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 120 });
                DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 120 });
            }

            DrawRectangle(posX: 0, posY: 0, width: level.Width * 20, height: level.Height * 20, Color.Red with { A = 40 });

            DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 210 });
            DrawTexture(Context.Viewports.Tiles[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 210 });

            DrawTexture(Context.Viewports.Props.Texture, posX: 0, posY: 0, tint: Color.White);

            DrawRectangle(posX: 0, posY: 0, Context.Viewports.Main.Width, Context.Viewports.Main.Height, Color.Magenta with { A = (byte)tintOpacity });
            
            DrawTexture(Context.Viewports.Effect.Texture, posX: 0, posY: 0, tint: Color.White);
            
            EndTextureMode();
            
            redrawMain = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, posX: 0, posY: 0, tint: Color.White);

        cursor.DrawCursor();

        for (var x = cursor.MX - brushSize; x < cursor.MX + brushSize + 1; x++)
        {
            for (var y = cursor.MY - brushSize; y < cursor.MY + brushSize + 1; y++)
            {
                if (CheckCollisionPointCircle(
                        point: (new Vector2(x, y) * 20) + (Vector2.One * 0.5f), 
                        center: (cursor.MXPos*20) + (Vector2.One * 0.5f), 
                        radius: (brushSize + 1) * 20)
                    )
                {
                    DrawRectangle(posX: x * 20, posY: y * 20, width: 20, height: 20, Color.White with { A = 30 });
                }
            }
        }
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (Context.SelectedLevel is not { } level) return;

        if (ImGui.Begin(name: "Menu##EffectMenu"))
        {
            if (selectedEffectDef is null) ImGui.BeginDisabled();
            if (ImGui.Button(label: "Add", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                level.Effects.Add(new Effect(selectedEffectDef!, level.Width, level.Height));
            }
            if (selectedEffectDef is null) ImGui.EndDisabled();

            if (ImGui.BeginListBox(label: "##Effects", size: ImGui.GetContentRegionAvail()))
            {
                foreach (var category in Context.Effects.Categories)
                {
                    ImGui.SeparatorText(category);

                    foreach (var effect in Context.Effects.CategoryEffects[category])
                    {
                        if (ImGui.Selectable(label: $"{effect.Name}##{effect.ID}", selected: effect == selectedEffectDef))
                        {
                            selectedEffectDef = effect;
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin(name: "Effects##EffectList"))
        {
            var disableRemove = selectedEffect is null;

            if (disableRemove) ImGui.BeginDisabled();
            if (ImGui.Button(label: "Remove", size: ImGui.GetContentRegionAvail() with { Y = 20 }))
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

                redrawEffects = true;
            }
            if (disableRemove) ImGui.EndDisabled();

            if (ImGui.BeginListBox(label: "##Effects", size: ImGui.GetContentRegionAvail()))
            {
                for (var e = 0; e < level.Effects.Count; e++)
                {
                    var effect = level.Effects[e];

                    if (ImGui.Selectable(label: $"{e}. {effect.Def.Name}", selected: effect == selectedEffect))
                    {
                        selectedEffect = effect;
                        redrawEffects = true;
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin(name: "Settings##EffectEditorSettings"))
        {
            if (ImGui.SliderInt(label: "Tint Opacity", v: ref tintOpacity, v_min: 0, v_max: 250))
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