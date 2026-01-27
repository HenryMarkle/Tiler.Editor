namespace Tiler.Editor.Tile;

using System.IO;
using IniParser;

public abstract class TileDef(string id, string resourceDir)
{
    public string ID { get; private set; } = id;
    public string ResourceDir { get; set; } = resourceDir;

    public string? Name { get; set; }
    public string? Category { get; set; }
    public Color4 Color { get; set; }
    public int Depth { get; set; } = 10;

    public static bool operator==(TileDef lhs, TileDef? rhs) => lhs.Equals(rhs);
    public static bool operator!=(TileDef lhs, TileDef? rhs) => !lhs.Equals(rhs);

    public override bool Equals(object? obj) => obj is TileDef tile && GetHashCode() == tile.GetHashCode();
    public override int GetHashCode() => ID.GetHashCode();
    public override string ToString() => $"Tile({ID})";

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

        var id = data["id"];
        var name = data["name"];
        var category = data["category"];
        Color4 color = data["color"]?.ToColor4() ?? new Color4(120, 120, 120);
        var depth = data["depth"]?.ToInt() ?? 10;

        var type = data["type"];

        switch (type)
        {
            case "Custom":
            {
                var scriptFile = Path.Combine(dir, "script.lua");

                if (!File.Exists(scriptFile))
                    throw new TileParseException($"'script.lua' file not found");

                return new CustomTileDef(id, resourceDir: dir)
                {
                    Name = name,
                    Category = category,
                    Color = color,
                    Depth = depth
                };
            }

            default: throw new TileParseException($"Unknown tile type '{type}'");
        }
    }
}

public sealed class CustomTileDef(string id, string resourceDir) : TileDef(id, resourceDir)
{
    public string ScriptFile { get; init; } = Path.Combine(resourceDir, "script.lua");

    public override string ToString() => $"CustomTile({ID})";
}
