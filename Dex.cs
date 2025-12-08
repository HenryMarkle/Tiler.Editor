namespace Tiler.Editor;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Tiler.Editor.Tile;

public class TileDex
{
    public readonly Dictionary<string, TileDef> Tiles = [];
    public readonly Dictionary<string, List<TileDef>> CategoryTiles = [];
    public readonly List<string> Categories = [];
    public readonly List<TileDef> UnCategorizedTiles = [];

    /// <summary>
    /// Registers a tile.
    /// </summary>
    /// <returns>Returns true if added successfully, false if already exists.</returns>
    /// <exception cref="DuplicateTileException"></exception>
    public void Register(TileDef tile)
    {
        if (!Tiles.TryAdd(tile.ID, tile))
            throw new DuplicateTileException(tile.ID);

        if (string.IsNullOrEmpty(tile.Category))
            UnCategorizedTiles.Add(tile);
        else
        {
            if (!CategoryTiles.TryGetValue(tile.Category, out var tiles))
            {
                CategoryTiles.Add(tile.Category, [ tile ]);
                Categories.Add(tile.Category);
            }
            else
                tiles.Add(tile);
        }
    }

    public void Register(string dir)
    {
        if (!Directory.Exists(dir))
            throw new TilerException($"Tile directory not found");

        try
        {
            var tile = TileDef.FromDir(dir);
            Register(tile);
        }
        catch (TilerException te)
        {
            throw new TileParseException($"Failed to parse tile", te);
        }
    }

    public static TileDex FromTilesDir(string dir)
    {
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException();

        var dex = new TileDex();

        foreach (var folder in Directory.GetDirectories(dir))
        {
            var iniFile = Path.Combine(folder, "tile.ini");
            if (!File.Exists(iniFile)) continue;

            try
            {
                dex.Register(folder);
            }
            catch (DuplicateTileException dte)
            {
                Log.Warning("Skipping duplicate tile {Tile}", dte.TileID);
                continue;
            }
            catch (TilerException te)
            {
                Log.Error(
                    "Failed to load tile in directory '{Name}'\n{Exception}", 
                    Path.GetFileNameWithoutExtension(folder), 
                    te
                );
                continue;
            }
        }

        return dex;
    }
}