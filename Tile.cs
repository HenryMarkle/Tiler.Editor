namespace Tiler.Editor.Tile;

using System.IO;
using IniParser;

public enum TileType : byte
{
    Custom
}

public abstract class TileDef(string id, TileType type, string resourceDir)
{
    public string ID { get; private set; } = id;
    public TileType Type { get; init; } = type;
    public string ResourceDir { get; set; } = resourceDir;

    public string? Name { get; set; }
    public string? Category { get; set; }
    public Color4 Color { get; set; }
    public int Depth { get; set; } = 10;

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

        var data = parser.ReadFile(file)["tile"];

        var id = data["id"] ?? throw new TileParseException("Required 'id' key");
        var name = data["name"];
        var category = data["category"];
        Color4 color = data["color"]?.ToColor4() ?? new Color4(120, 120, 120);
        var depth = data["depth"]?.ToInt() ?? 10;

        var typeStr = data["type"];

        switch (typeStr)
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

            default: throw new TileParseException($"Unknown tile type '{typeStr}'");
        }
    }
}

public sealed class CustomTileDef(string id, string resourceDir) : TileDef(id, TileType.Custom, resourceDir)
{
    public string ScriptFile { get; init; } = Path.Combine(resourceDir, "script.lua");

    public override string ToString() => $"CustomTile({ID})";
}
