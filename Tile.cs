using System;
using IniParser.Model;

namespace Tiler.Editor.Tile;

using System.IO;
using IniParser;

public abstract class TileDef(string id, string resourceDir) 
    : IEquatable<TileDef>, IResource, IIdentifiable<string>, IOrganizable
{
    public string ID { get; } = id;
    public string ResourceDir { get; } = resourceDir;

    public string? Name { get; set; }
    public string? Category { get; set; }
    public Color4 Color { get; set; }
    public int Depth { get; set; } = 10;

    public static bool operator==(TileDef? lhs, TileDef? rhs) => lhs is not null && lhs.Equals(rhs);
    public static bool operator!=(TileDef? lhs, TileDef? rhs) => lhs is null || !lhs.Equals(rhs);

    public override bool Equals(object? obj) => obj is TileDef tile && Equals(tile);

    public override int GetHashCode() => ID.GetHashCode();

    public override string ToString() => $"Tile({ID})";

    protected abstract void OnLoad(KeyDataCollection data, string dir);

    // TODO: re-implement this shit (OCP violation)
    /// <summary>
    /// Parses a tile from a tile entry.
    /// </summary>
    /// <param name="dir">The folder containing 'tile.ini'</param>
    /// <exception cref="TileParseException"></exception>
    public static TileDef FromDir(string dir)
    {
        var file = Path.Combine(dir, "tile.ini");

        if (!File.Exists(file))
            throw new TileParseException("'tile.ini' not found");

        var parser = new FileIniDataParser();

        var data = parser.ReadFile(file).Global;

        if (!data.ContainsKey("id"))
            throw new TileParseException("Required 'id' key");
        
        if (!data.ContainsKey("type"))
            throw new TileParseException("Required 'type' key");

        var id       = data["id"]!;
        var name            = data["name"];
        var category = data["category"];
        var color           = data["color"]?.ToColor4() ?? new Color4(r: 120, g: 120, b: 120);
        var depth        = data["depth"]?.ToInt() ?? 10;

        TileDef tile = data["type"] switch
        {
            "Custom" => new CustomTileDef(id, dir),
            
            var type => throw new TileParseException($"Unknown tile type '{type}'"),
        };

        tile.Name     = name;
        tile.Category = category;
        tile.Color    = color;
        tile.Depth    = depth;

        tile.OnLoad(data, dir);

        return tile;
    }

    public bool Equals(TileDef? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ID == other.ID;
    }
}

public sealed class CustomTileDef(string id, string resourceDir) : TileDef(id, resourceDir)
{
    public string ScriptFile { get; init; } = Path.Combine(resourceDir, "script.lua");

    public override string ToString() => $"CustomTile({ID})";

    protected override void OnLoad(KeyDataCollection data, string dir)
    {
        if (!File.Exists(ScriptFile))
            throw new TileParseException($"'script.lua' file not found");
    }
}
