using System.Collections.Generic;
using System.Linq;
using Tiler.Editor.Rendering.Scripting;
using Tiler.Editor.Tile;
using Tiler.Editor.Views;

namespace Tiler.Editor.Rendering;

public class TileRenderer
{
    private const int Width = 1400;
    private const int Height = 800;

    private readonly Managed.RenderTexture[] layers;
    public readonly Level level;
    private readonly TileDex tiles;
    private readonly int sublayersPerLayer;
    private readonly int layerMargin;
    private readonly LevelCamera camera;

    private readonly List<Dictionary<TileDef, List<(int mx, int my)>>> cells;
    public bool IsDone { get; private set; }


    public TileRenderer(Managed.RenderTexture[] layers, Level level, TileDex tiles, LevelCamera camera)
    {
        this.layers = layers;
        this.level = level;
        this.tiles = tiles;
        this.camera = camera;
        layerMargin = 100;
        sublayersPerLayer = 10;
        IsDone = false;

        var columns = (Width + layerMargin) / 20;
        var rows = (Height + layerMargin) / 20;

        cells = Enumerable
            .Range(0, layers.Length / sublayersPerLayer)
            .Select(l =>
            {
                Dictionary<TileDef, List<(int x, int y)>> layerCells = [];

                for (var y = 0; y < rows; y++)
                {
                    var my = y + (int)(camera.Position.Y/20);
                    if (my < 0 || my >= level.Height) continue;

                    for (var x = 0; x < columns; x++)
                    {
                        var mx = x + (int)(camera.Position.X/20);
                        if (mx < 0 || mx >= level.Width) continue;

                        var cell = level.Tiles[x, y, l] ??= level.DefaultTile;
                        if (cell is null) continue;

                        if (!layerCells.TryAdd(cell, [(x, y)]))
                            layerCells[cell].Add((x, y));
                    }
                }

                return layerCells; 
            })
            .ToList();
    }

    // NOTE: temporary
    public void Next()
    {
        var layer = 0;
        foreach (var dict in cells)
        {
            foreach (var (tile, positions) in dict)
            {
                switch (tile)
                {
                    case CustomTileDef custom:
                    {
                        var script = new TileRenderingScriptRuntime(tile, custom.ScriptFile, level, layers);

                        foreach (var (mx, my) in positions)
                        {
                            script.ExecuteRender(mx, my, layer);   
                        }
                    } 
                    break;
                }
            }

            layer++;
        }

        IsDone = true;
    }
}