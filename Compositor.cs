// Unused; maybe in the future..

using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Tiler.Editor;

public class CompositorConfiguration
{
    public Color Background = Color.LightGray;

    public enum ColorSchemes { RGB, Gray }
    public readonly ColorSchemes ColorScheme = ColorSchemes.Gray;

    /// <summary>
    /// Covers the non current layers with a red tint, under the current one.
    /// </summary>
    public readonly bool RedOverlay = true;
    
    public readonly bool Grid = true;
    public readonly int GridSize = 20;
    public readonly bool DoubleGrid = true;

    public readonly int CurrentLayer = 0;

    public readonly bool Terrain = true;
    public readonly bool Tiles = true;
    public readonly List<LevelCamera>? Cameras;
    public readonly bool Props = true;

    public readonly byte PropsOpacity = 240;
}

public class Compositor(Viewports viewports)
{
    public Viewports Viewports { get; set; } = viewports;

    public CompositorConfiguration DefaultConfig = new();

    private static readonly Color[] cameraColors = [
        Color.Green with { A = 80 },
        Color.Red with { A = 80 },
        Color.Blue with { A = 80 },
        Color.Magenta with { A = 80 },
        Color.Orange with { A = 80 },
        Color.Gold with { A = 80 },
        Color.Gray with { A = 80 },
    ];

    public void Composite(CompositorConfiguration? config)
    {
        config ??= DefaultConfig;

        BeginTextureMode(Viewports.Main);
        ClearBackground(config.Background);

        if (config.Terrain)
        {
            switch (config.ColorScheme)
            {
                case CompositorConfiguration.ColorSchemes.Gray:
                    for (int l = Viewports.Depth - 1; l > -1; --l)
                    {
                        if (l == config.CurrentLayer) continue;
                        DrawTexture(Viewports.Geos[l].Raw.Texture, 0, 0, Color.Black with { A = 120 });
                        if (config.Tiles) DrawTexture(Viewports.Tiles[l].Raw.Texture, 0, 0, Color.White with { A = 120 });
                    }
                    
                    if (config.RedOverlay)
                        DrawRectangle(0, 0, Viewports.Main.Width * 20, Viewports.Main.Height * 20, Color.Red with { A = 40 });

                    DrawTexture(Viewports.Geos[config.CurrentLayer].Raw.Texture, 0, 0, Color.Black with { A = 210 });
                    if (config.Tiles) DrawTexture(Viewports.Tiles[config.CurrentLayer].Raw.Texture, 0, 0, Color.White with { A = 210 });
                    break;

                case CompositorConfiguration.ColorSchemes.RGB:
                    DrawTexture(Viewports.Geos[0].Raw.Texture, 0, 0, new Color(0, 0, 0, 255));
                    DrawTexture(Viewports.Geos[1].Raw.Texture, 0, 0, new Color(0, 255, 0, 80));
                    DrawTexture(Viewports.Geos[2].Raw.Texture, 0, 0, new Color(255, 0, 0, 80));
                    DrawTexture(Viewports.Geos[3].Raw.Texture, 0, 0, new Color(0, 0, 255, 80));
                    DrawTexture(Viewports.Geos[4].Raw.Texture, 0, 0, new Color(200, 200, 255, 80));

                    if (config.Tiles)
                    {
                        for (int l = Viewports.Depth - 1; l > -1; --l)
                        {
                            if (l == config.CurrentLayer) continue;
                            DrawTexture(Viewports.Tiles[l].Raw.Texture, 0, 0, Color.White with { A = 120 });
                        }
                        
                        DrawTexture(Viewports.Tiles[config.CurrentLayer].Raw.Texture, 0, 0, Color.White with { A = 210 });
                    }
                    break;
            }
        }

        if (config.Props)
        {
            
        }

        if (config.Cameras is not null)
        {
            for (var c = 0; c < config.Cameras.Count; c++)
            {
                var cam = config.Cameras[c];

                DrawRectangleLinesEx(
                    new Rectangle(cam.Position, new Vector2(LevelCamera.Width, LevelCamera.Height)),
                    1.2f,
                    cameraColors[c % cameraColors.Length]
                );
                
                DrawText($"{c}", (int)(cam.Position.X + 25), (int)(cam.Position.Y + 20), 20, Color.White);
            }
        }

        EndTextureMode();
    }
}