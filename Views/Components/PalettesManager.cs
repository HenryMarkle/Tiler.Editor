using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Raylib_cs;
using static Raylib_cs.Raylib;

using ImGuiNET;
using Tiler.Editor;
using System.Collections.Generic;
using Tiler.Editor.Managed;
using System.Linq;
using System.IO;

public class PalettesManager
{
    public Context Context { get; set; }

    public Dictionary<string, HybridImage> Palettes { get; set; }
    private (string name, HybridImage image)[] OrderedPalettes { get; set; }
    public HybridImage? SelectedPalette { get; private set; }
    public int SelectedPaletteIndex { get; private set; }

    public delegate void PaletteSelectedEventHandler(string key, HybridImage image);
    public event PaletteSelectedEventHandler? PaletteSelected;

    public PalettesManager(Context context)
    {
        Context = context;

        OrderedPalettes = Directory
            .GetFiles(context.Dirs.Palettes)
            .Where(f => f.EndsWith(".png"))
            .AsParallel()
            .Select(f => (Path.GetFileNameWithoutExtension(f), new HybridImage(LoadImage(f))))
            .OrderBy(f => f.Item1)
            .ToArray();

        Palettes = OrderedPalettes.ToDictionary();

        SelectedPalette = Palettes.FirstOrDefault().Value;
        SelectedPaletteIndex = SelectedPalette is null ? -1 : 0;
    }

    public void Select(string name)
    {
        if (!Palettes.TryGetValue(name, out var image))
            return;

        SelectedPalette = image;
        SelectedPaletteIndex = Array.FindIndex(OrderedPalettes, p => p.name == name);

        PaletteSelected?.Invoke(name, image);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= OrderedPalettes.Length)
            return;

        var (name, palette) = OrderedPalettes[index];
        
        SelectedPalette = palette;
        SelectedPaletteIndex = index;

        PaletteSelected?.Invoke(name, palette);
    }

    public void DrawPalettesMenuGUI()
    {
        if (ImGui.Begin("Palettes##PalettesManagerMenu"))
        {
            if (ImGui.BeginListBox("##Menu", ImGui.GetContentRegionAvail()))
            {
                for (var p = 0; p < OrderedPalettes.Length; p++)
                {
                    var (_, image) = OrderedPalettes[p];

                    image.ToTexture();

                    var space = ImGui.GetContentRegionAvail();
                    var ratio = space with { X = space.X - 10 } / image.Size;
                    var minRatio = MathF.Min(ratio.X, ratio.Y);
                    var imageSize = image.Size * minRatio;

                    var isSelected = SelectedPaletteIndex == p;

                    ImGui.BeginGroup();

                    var pos = ImGui.GetCursorScreenPos();

                    if (ImGui.Selectable($"##{p}", isSelected, ImGuiSelectableFlags.None, ImGui.GetContentRegionAvail() with { Y = imageSize.Y }))
                    {
                        Select(p);
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
        }

        ImGui.End();
    }
}