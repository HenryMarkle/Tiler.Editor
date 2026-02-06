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

public class Palettes : BaseView
{
    private readonly Cursor cursor;

    private HybridImage? selectedLevel;
    private HybridImage paletteImage;

    private HybridImage[] levels;

    private readonly Managed.Shader paletteShader;

    private int general;
    private int fogIntensity;
    private int layer;
    private int value;
    private bool sunlit;
    private bool updatePaletteCanvas = true;

    private bool openImport;
    private bool openExport;

    private (string name, HybridImage image)[] orderedPalettes = [];

    private string exportName = "";
    private bool canExport;

    private void ReloadPalettes()
    {
        orderedPalettes = Directory
            .GetFiles(Context.Dirs.Palettes)
            .Where(f => f.EndsWith(".png"))
            .AsParallel()
            .Select(f => (Path.GetFileNameWithoutExtension(f), new HybridImage(LoadImage(f))))
            .OrderBy(f => f.Item1)
            .ToArray();
    }

    public Palettes(Context context) : base(context)
    {
        cursor = new Cursor(context);

        levels = Directory
            .GetDirectories(context.Dirs.Levels)
            .SelectMany(l => 
                Directory
                    .GetFiles(l)
                    .Where(f => f.EndsWith(".png"))
                    .Select(i => new HybridImage(LoadImage(i))))
            .ToArray();

        selectedLevel = levels.FirstOrDefault();

        paletteImage = new HybridImage(GenImageColor(50, 8, Color.White));

        ImageFormat(ref paletteImage.Image, PixelFormat.UncompressedR8G8B8A8);

        for (var layer = 0; layer < 50; layer++)
        {
            // BeginTextureMode(paletteCanvas);
            // DrawRectangle(layer, 7 - 1, 1, 1, new Color(200, 0, 0, 255));
            // DrawRectangle(layer, 7 - 2, 1, 1, new Color(0, 200, 0, 255));
            // DrawRectangle(layer, 7 - 3, 1, 1, new Color(0, 0, 200, 255));

            // DrawRectangle(layer, 7 - 3 - 1, 1, 1, new Color(255, 0, 0, 255));
            // DrawRectangle(layer, 7 - 3 - 2, 1, 1, new Color(0, 255, 0, 255));
            // DrawRectangle(layer, 7 - 3 - 3, 1, 1, new Color(0, 0, 255, 255));
            // EndTextureMode();

            ImageDrawPixel(ref paletteImage.Image, layer, 1, new Color(200, 0, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, layer, 2, new Color(0, 200, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, layer, 3, new Color(0, 0, 200, 255));

            ImageDrawPixel(ref paletteImage.Image, layer, 1 + 3, new Color(255, 0, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, layer, 2 + 3, new Color(0, 255, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, layer, 3 + 3, new Color(0, 0, 255, 255));
        }

        // BeginTextureMode(paletteCanvas);
        // DrawRectangle(0, 7 - 0, 1, 1, new Color(255, 0, 0, 255));
        // DrawRectangle(1, 7 - 0, 1, 1, new Color(255, 255, 255, 255));
        // DrawRectangle(3, 7 - 0, 1, 1, new Color(100, 0, 0, 255));
        // EndTextureMode();

        ImageDrawPixel(ref paletteImage.Image, 0, 0, new Color(255, 0, 0, 255));
        ImageDrawPixel(ref paletteImage.Image, 1, 0, new Color(255, 255, 255, 255));
        ImageDrawPixel(ref paletteImage.Image, 3, 0, new Color(100, 0, 0, 255));

        paletteShader = new Managed.Shader(LoadShaderFromMemory(null, @"
        #version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform sampler2D palette;

out vec4 finalColor;

vec4 white = vec4(1, 1, 1, 1);
vec4 black = vec4(0, 0, 0, 1);

vec2 darkPos = vec2(0.1 / 50.0, 0);
vec2 skyPos = vec2(1.1 / 50.0, 0);
vec2 fogPos = vec2(2.1 / 50.0, 0);
vec2 fogIntenPos = vec2(3.1 / 50.0, 0);

void main() {
    vec4 pixel = texture(texture0, vec2(fragTexCoord.x, 1.0 - fragTexCoord.y));

    if (pixel == black) {   // darkness
        finalColor = texture(palette, darkPos);
        return;
    }

    if (pixel == white) {   // sky
        finalColor = texture(palette, skyPos);
        return;
    }

    int red = int(pixel.r * 255);

    int isSunlit = int(red > 50);
    int depth = red - (isSunlit * 50);
    float value = pixel.g;

    int valueRow = 0;
    if (value >= 0.3 && value <= 0.8) valueRow = 1;
    else if (value >= 0.988) valueRow = 2; 

    vec4 fog = texture(palette, fogPos);
    float fogIntensity = texture(palette, fogIntenPos).r;

    vec4 layerColor = texture(palette, vec2(depth / 50.0, (1 + (isSunlit * 3) + valueRow) / 8.0));

    finalColor = mix(layerColor, fog, fogIntensity * (depth / 50.0));
}
"));
    }

    public override void OnViewSelected()
    {
        base.OnViewSelected();
    }

    public override void Process()
    {
        if (!cursor.IsInWindow) cursor.ProcessCursor();
    }

    public override void Draw()
    {
        if (selectedLevel is null) return;

        if (updatePaletteCanvas)
        {
            paletteImage.ToTexture();

            selectedLevel.ToTexture();
            Context.Viewports.Main.Clear();

            BeginTextureMode(Context.Viewports.Main);
            BeginShaderMode(paletteShader);
            paletteShader.SetTexture("texture0", selectedLevel);
            paletteShader.SetTexture("palette", paletteImage.Texture);

            DrawTexture(selectedLevel, 0, 0, Color.White);
            EndShaderMode();
            EndTextureMode();

            paletteImage.ToImage();

            updatePaletteCanvas = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, 0, 0, Color.White);
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin("Editor##PaletteEditor"))
        {
            // TODO: Handle buttons
            if (ImGui.Button("Import", ImGui.GetContentRegionAvail() with { Y = 20 })) openImport = true;
            ImGui.Button("Export", ImGui.GetContentRegionAvail() with { Y = 20 });

            ImGui.Spacing();

            paletteImage.ToTexture();

            var space = ImGui.GetContentRegionAvail();
            var ratio = space with { X = space.X - 10 } / paletteImage.Size;
            var minRatio = MathF.Min(ratio.X, ratio.Y);
            var imageSize = paletteImage.Size * minRatio;

            rlImGui_cs.rlImGui.ImageSize(paletteImage.Texture, imageSize);

            paletteImage.ToImage();

            ImGui.Spacing();

            if (ImGui.BeginTabBar("EditSections"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Combo("Section", ref general, "Darkness\0Sky\0Fog");
                    if (ImGui.SliderInt("Fog Intensity", ref fogIntensity, 0, 255))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            3, 
                            0, 
                            new Color(fogIntensity, 0, 0)
                        );

                        updatePaletteCanvas = true;
                    }

                    var color = GetImageColor(paletteImage, general, 0);
                    var colorV3 = new Vector3(color.R/255.0f, color.G/255.0f, color.B/255.0f);

                    if (ImGui.ColorPicker3("##LayerColor", ref colorV3))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            general, 
                            0, 
                            new Color((byte)(colorV3.X * 255), (byte)(colorV3.Y * 255), (byte)(colorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Layers"))
                {
                    if (ImGui.InputInt("Layer", ref layer))
                    {
                        layer = Math.Clamp(layer, 0, 49);
                    }

                    ImGui.Combo("Value", ref value, "Shadow\0Base\0Highlit");
                    ImGui.Checkbox("Sunlit", ref sunlit);

                    var layerColor = GetImageColor(paletteImage, layer, 1 + value + (sunlit ? 3 : 0));
                    var layerColorV3 = new Vector3(layerColor.R/255.0f, layerColor.G/255.0f, layerColor.B/255.0f);

                    if (ImGui.Button("Copy to next layer", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            Math.Clamp(layer + 1, 0, 49), 
                            1 + value + (sunlit ? 3 : 0), 
                            new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255), (byte)(layerColorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button("Copy to all layers", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawRectangle(
                            ref paletteImage.Image, 
                            0, 
                            1 + value + (sunlit ? 3 : 0), 
                            50,
                            1,
                            new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255), (byte)(layerColorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button("Copy to all", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawRectangle(
                            ref paletteImage.Image, 
                            0, 
                            1, 
                            50,
                            6,
                            new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255), (byte)(layerColorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.ColorPicker3("##LayerColor", ref layerColorV3))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            layer, 
                            1 + value + (sunlit ? 3 : 0), 
                            new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255), (byte)(layerColorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();


        if (ImGui.Begin("Renders##PaletteEditorLevels"))
        {
            if (ImGui.BeginListBox("##Levels", ImGui.GetContentRegionAvail()))
            {
                for (var level = 0; level < levels.Length; level++)
                {
                    var image = levels[level];
                    image.ToTexture();

                    var space = ImGui.GetContentRegionAvail();
                    var ratio = space with { X = space.X - 10 } / image.Size;
                    var minRatio = MathF.Min(ratio.X, ratio.Y);
                    var imageSize = image.Size * minRatio;

                    var pos = ImGui.GetCursorScreenPos();

                    if (ImGui.Selectable($"##{level}", selectedLevel == image, ImGuiSelectableFlags.None, ImGui.GetContentRegionAvail() with { Y = imageSize.Y }))
                    {
                        selectedLevel = image;
                    }

                    ImGui.SetCursorScreenPos(pos);
                    rlImGui_cs.rlImGui.ImageSize(image, imageSize);
                } 

                ImGui.EndListBox();
            }
        }

        ImGui.End();


        if (openImport) {
            ReloadPalettes();
            ImGui.OpenPopup("Import##PaletteImportPopup");
            openImport = false;
        }

        if (ImGui.BeginPopupModal("Import##PaletteImportPopup", ImGuiWindowFlags.NoCollapse))
        {
            var avail = ImGui.GetContentRegionAvail();
            if (ImGui.BeginListBox("##Menu", avail with { Y = avail.Y - 30 }))
            {
                for (var p = 0; p < orderedPalettes.Length; p++)
                {
                    var (_, image) = orderedPalettes[p];

                    image.ToTexture();

                    var space = ImGui.GetContentRegionAvail();
                    var ratio = space with { X = space.X - 10 } / image.Size;
                    var minRatio = MathF.Min(ratio.X, ratio.Y);
                    var imageSize = image.Size * minRatio;

                    ImGui.BeginGroup();

                    var pos = ImGui.GetCursorScreenPos();

                    if (ImGui.Selectable($"##{p}", false, ImGuiSelectableFlags.None, ImGui.GetContentRegionAvail() with { Y = imageSize.Y }))
                    {
                        paletteImage = image;
                        updatePaletteCanvas = true;
                        ImGui.CloseCurrentPopup();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        rlImGui_cs.rlImGui.ImageSize(image, image.Size * 10);
                        ImGui.EndTooltip();
                    }

                    ImGui.SetCursorScreenPos(pos);
                    rlImGui_cs.rlImGui.ImageSize(image, imageSize);

                    ImGui.EndGroup();
                }

                ImGui.EndListBox();
            }

            if (ImGui.Button("Cancel", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (openExport)
        {
            ImGui.OpenPopup("Export##PaletteExportPopup");
            openExport = false;
        }

        if (ImGui.BeginPopupModal("Export##PaletteExportPopup", ImGuiWindowFlags.NoCollapse))
        {
            if (!canExport)
            {
                ImGui.TextColored(
                    new Vector4(1f, 0.1f, 0.1f, 1f), 
                    (string.IsNullOrEmpty(exportName) || string.IsNullOrWhiteSpace(exportName)) ? "Name can't be empty" : "Name already exists"
                );
            }
            if (ImGui.InputText("Name", ref exportName, 256))
            {
                canExport = !string.IsNullOrEmpty(exportName) && 
                    !string.IsNullOrWhiteSpace(exportName) && 
                    Directory
                    .GetFiles(Context.Dirs.Palettes)
                    .All(f => Path.GetFileNameWithoutExtension(f) != exportName);
            }

            if (!canExport) ImGui.BeginDisabled();
            if (ImGui.Button("Export", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                paletteImage.ToImage();
                ExportImage(paletteImage.Image, exportName);

                ImGui.CloseCurrentPopup();
            }
            if (!canExport) ImGui.EndDisabled();

            if (ImGui.Button("Cancel", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    public override void Debug()
    {
        cursor.PrintDebug();
    }
}