namespace Tiler.Editor.Views;

using System;
using System.IO;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Tiler.Editor.Managed;
using Tiler.Editor.Views.Components;

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
    private bool duplicateExportName;

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

        ImageFormat(ref paletteImage.Image, PixelFormat.UncompressedR8G8B8);

        for (var lyr = 0; lyr < 50; lyr++)
        {
            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 1, new Color(200, 0, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 2, new Color(0, 200, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 3, new Color(0, 0, 200, 255));

            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 1 + 3, new Color(255, 0, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 2 + 3, new Color(0, 255, 0, 255));
            ImageDrawPixel(ref paletteImage.Image, posX: lyr, posY: 3 + 3, new Color(0, 0, 255, 255));
        }
        
        ImageDrawPixel(ref paletteImage.Image, posX: 0, posY: 0, new Color(255, 0, 0, 255));
        ImageDrawPixel(ref paletteImage.Image, posX: 1, posY: 0, new Color(255, 255, 255, 255));
        ImageDrawPixel(ref paletteImage.Image, posX: 3, posY: 0, new Color(100, 0, 0, 255));

        paletteShader = new Managed.Shader(LoadShaderFromMemory(
            vsCode: null, 
            fsCode: """
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
"""
            ));
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
            paletteShader.SetTexture(uniformName: "texture0", selectedLevel);
            paletteShader.SetTexture(uniformName: "palette", paletteImage.Texture);

            DrawTexture(selectedLevel, posX: 0, posY: 0, tint: Color.White);
            EndShaderMode();
            EndTextureMode();

            paletteImage.ToImage();

            updatePaletteCanvas = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(Context.Viewports.Main.Texture, posX: 0, posY: 0, tint: Color.White);
        EndMode2D();
    }

    public override void GUI()
    {
        cursor.ProcessGUI();

        if (ImGui.Begin(name: "Editor##PaletteEditor"))
        {
            // TODO: Handle buttons
            if (ImGui.Button(label: "Import", ImGui.GetContentRegionAvail() with { Y = 20 })) openImport = true;
            if (ImGui.Button(label: "Export", ImGui.GetContentRegionAvail() with { Y = 20 })) openExport = true;

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
                if (ImGui.BeginTabItem(label: "General"))
                {
                    ImGui.Combo(label: "Section", ref general, "Darkness\0Sky\0Fog");
                    if (ImGui.SliderInt(label: "Fog Intensity", v: ref fogIntensity, v_min: 0, v_max: 255))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            posX: 3, 
                            posY: 0, 
                            new Color(fogIntensity, 0, 0)
                        );

                        updatePaletteCanvas = true;
                    }

                    var color = GetImageColor(paletteImage, x: general, y: 0);
                    var colorV3 = new Vector3(color.R/255.0f, color.G/255.0f, color.B/255.0f);

                    if (ImGui.ColorPicker3(label: "##LayerColor", ref colorV3))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            posX: general, 
                            posY: 0, 
                            new Color((byte)(colorV3.X * 255), (byte)(colorV3.Y * 255), (byte)(colorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(label: "Layers"))
                {
                    if (ImGui.InputInt(label: "Layer", ref layer))
                    {
                        layer = Math.Clamp(layer, 0, 49);
                    }

                    ImGui.Combo(label: "Value", ref value, "Shadow\0Base\0Highlight");
                    ImGui.Checkbox(label: "Sunlit", ref sunlit);

                    var layerColor = GetImageColor(paletteImage, x: layer, y: 1 + value + (sunlit ? 3 : 0));
                    var layerColorV3 = new Vector3(layerColor.R/255.0f, layerColor.G/255.0f, layerColor.B/255.0f);
                    var layerColor3 = new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255),
                        (byte)(layerColorV3.Z * 255));

                    if (ImGui.Button(label: "Copy to next layer", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            posX: Math.Clamp(layer + 1, 0, 49), 
                            posY: 1 + value + (sunlit ? 3 : 0), 
                            layerColor3
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button(label: "Copy to sublayer space", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawRectangle(
                            ref paletteImage.Image,
                            posX: layer / 10 * 10,
                            posY: 1 + value + (sunlit ? 3 : 0),
                            width: 10,
                            height: 1,
                            layerColor3
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button(label: "Copy to all layers", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawRectangle(
                            ref paletteImage.Image, 
                            posX: 0, 
                            posY: 1 + value + (sunlit ? 3 : 0), 
                            width: 50,
                            height: 1,
                            layerColor3
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button(label: "Copy all shadow to sunlit", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        for (var x = 0; x < 49; x++)
                        {
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 0 + 3, 
                                GetImageColor(paletteImage, x, y: 1 + 0)
                            );
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 1 + 3, 
                                GetImageColor(paletteImage, x, y: 1 + 1)
                            );
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 2 + 3, 
                                GetImageColor(paletteImage, x, y: 1 + 2)
                            );
                        }
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button(label: "Copy all sunlit to shadow", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        for (var x = 0; x < 49; x++)
                        {
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 0, 
                                GetImageColor(paletteImage, x, y: 1 + 0 + 3)
                            );
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 1, 
                                GetImageColor(paletteImage, x, y: 1 + 1 + 3)
                            );
                            ImageDrawPixel(
                                ref paletteImage.Image, 
                                posX: x, 
                                posY: 1 + 2, 
                                GetImageColor(paletteImage, x, y: 1 + 2 + 3)
                            );
                        }
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.Button(label: "Copy to all", ImGui.GetContentRegionAvail() with { Y = 20 }))
                    {
                        ImageDrawRectangle(
                            ref paletteImage.Image, 
                            posX: 0, 
                            posY: 1, 
                            width: 50,
                            height: 6,
                            layerColor3
                        );
                        updatePaletteCanvas = true;
                    }

                    if (ImGui.ColorPicker3(label: "##LayerColor", ref layerColorV3))
                    {
                        ImageDrawPixel(
                            ref paletteImage.Image, 
                            posX: layer, 
                            posY: 1 + value + (sunlit ? 3 : 0), 
                            new Color((byte)(layerColorV3.X * 255), (byte)(layerColorV3.Y * 255),
                                (byte)(layerColorV3.Z * 255))
                        );
                        updatePaletteCanvas = true;
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.End();


        if (ImGui.Begin(name: "Renders##PaletteEditorLevels"))
        {
            if (ImGui.BeginListBox(label: "##Levels", size: ImGui.GetContentRegionAvail()))
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

                    if (ImGui.Selectable(label: $"##{level}", selectedLevel == image, ImGuiSelectableFlags.None, ImGui.GetContentRegionAvail() with { Y = imageSize.Y }))
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

        if (ImGui.BeginPopupModal(name: "Import##PaletteImportPopup", ImGuiWindowFlags.NoCollapse))
        {
            var avail = ImGui.GetContentRegionAvail();
            if (ImGui.BeginListBox(label: "##Menu", avail with { Y = avail.Y - 30 }))
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

                    if (ImGui.Selectable(
                            label: $"##{p}", 
                            selected: false, 
                            ImGuiSelectableFlags.None, 
                            ImGui.GetContentRegionAvail() with { Y = imageSize.Y })
                        ) {
                        paletteImage = image;
                        updatePaletteCanvas = true;
                        ImGui.CloseCurrentPopup();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        rlImGui_cs.rlImGui.ImageSize(image, size: image.Size * 10);
                        ImGui.EndTooltip();
                    }

                    ImGui.SetCursorScreenPos(pos);
                    rlImGui_cs.rlImGui.ImageSize(image, imageSize);

                    ImGui.EndGroup();
                }

                ImGui.EndListBox();
            }

            if (ImGui.Button(label: "Cancel", ImGui.GetContentRegionAvail() with { Y = 20 }))
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

        if (ImGui.BeginPopupModal(name: "Export##PaletteExportPopup", ImGuiWindowFlags.NoCollapse))
        {
            if (ImGui.InputText(label: "Name", input: ref exportName, maxLength: 256))
            {
                duplicateExportName =
                    Directory
                        .GetFiles(Context.Dirs.Palettes)
                        .Any(f => Path.GetFileNameWithoutExtension(f) == exportName);
            }
            
            if (string.IsNullOrWhiteSpace(exportName)) ImGui.BeginDisabled();

            if (ImGui.Button(
                    label: duplicateExportName ? "Replace" : "Export", 
                    size: ImGui.GetContentRegionAvail() with { Y = 20 })
                ) {
                paletteImage.ToImage();
                ExportImage(paletteImage.Image, exportName);

                ImGui.CloseCurrentPopup();
            }
            if (string.IsNullOrWhiteSpace(exportName)) ImGui.EndDisabled();

            if (ImGui.Button(label: "Cancel", size: ImGui.GetContentRegionAvail() with { Y = 20 }))
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