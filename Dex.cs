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

public class PropDex
{
    public readonly Dictionary<string, PropDef> Props = [];
    public readonly Dictionary<string, List<PropDef>> CategoryProps = [];
    public readonly List<string> Categories = [];
    public readonly List<PropDef> UnCategorizedProps = [];

    /// <summary>
    /// Registers a tile.
    /// </summary>
    /// <returns>Returns true if added successfully, false if already exists.</returns>
    /// <exception cref="DuplicateTileException"></exception>
    public void Register(PropDef prop)
    {
        if (!Props.TryAdd(prop.ID, prop))
            throw new DuplicateTileException(prop.ID);

        if (string.IsNullOrEmpty(prop.Category))
        {
            Log.Verbose("[PropDex.Register] Prop is uncategorized");
            
            UnCategorizedProps.Add(prop);
        }
        else
        {
            if (!CategoryProps.TryGetValue(prop.Category, out var props))
            {
                Log.Verbose("[PropDex.Register] Creating a new category for the prop");
                
                CategoryProps.Add(prop.Category, [ prop ]);
                Categories.Add(prop.Category);
            }
            else
            {
                Log.Verbose("[PropDex.Register] Prop categorized");

                props.Add(prop);
            }
        }
    }

    public void Register(string dir)
    {
        if (!Directory.Exists(dir))
            throw new TilerException($"Prop directory not found");

        try
        {
            var prop = PropDef.FromDir(dir);
            Register(prop);
        }
        catch (TilerException pe)
        {
            throw new TileParseException($"Failed to parse prop", pe);
        }
    }

    public static PropDex FromPropsDir(string dir)
    {
        Log.Verbose("[PropDex.FromPropsDir] Registering props from directory '{Path}'", dir);

        if (!Directory.Exists(dir))
        {
            Log.Verbose("[PropDex.FromPropsDir] Directory was not found; throwing an exception.");
            throw new DirectoryNotFoundException();
        }

        var dex = new PropDex();

        Log.Verbose("[PropDex.FromPropsDir] Iterating over designated prop directory:");

        foreach (var folder in Directory.GetDirectories(dir))
        {
            var iniFile = Path.Combine(folder, "prop.ini");
            if (!File.Exists(iniFile))
            {
                Log.Verbose("[PropDex.FromPropsDir] \t-> Skipping {Name}/ (no prop.ini)", Path.GetFileName(folder));
                continue;
            }

            try
            {
                Log.Verbose("[PropDex.FromPropsDir] \t-> Trying {Name}/", Path.GetFileName(folder));
                dex.Register(folder);
                Log.Verbose("[PropDex.FromPropsDir] \t   Succeeded");
            }
            catch (DuplicatePropException dpe)
            {
                Log.Verbose("[PropDex.FromPropsDir] \t   Failed");
                Log.Warning("Skipping duplicate prop {Prop}", dpe.PropID);
                continue;
            }
            catch (TilerException te)
            {
                Log.Verbose("[PropDex.FromPropsDir] \t   Failed");
                Log.Error(
                    "Failed to load prop in directory '{Name}'\n{Exception}", 
                    Path.GetFileNameWithoutExtension(folder), 
                    te
                );
                continue;
            }
        }

        return dex;
    }
}