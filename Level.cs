using Tiler.Editor.Tile;

using System.IO;
using System.Numerics;
using IniParser;
using IniParser.Model;
using System.Collections.Generic;
using ImGuiNET;
using Raylib_cs;

namespace Tiler.Editor;

public class Level
{
    public const int DefaultWidth = 70;
    public const int DefaultHeight = 40;

    public string? Name = "New Level";
    public string? Directory;
    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;
    public int Depth { get; private set; } = 3;

    /// TODO: Use Either<> instead 
    public TileDef? DefaultTile;
    public Managed.Image Lightmap = new(Raylib.GenImageColor(DefaultWidth + 200, DefaultHeight + 200, new Color(0,0,0,0)));

    public Matrix<Geo> Geos = new(DefaultWidth, DefaultHeight, 3);
    public Matrix<TileDef?> Tiles = new(DefaultWidth, DefaultHeight, 30);
    public List<LevelCamera> Cameras = [ new LevelCamera(new Vector2(20, 20)) ];

    public void Resize(int width, int height)
    {
        Geos.Resize(width, height);
        Tiles.Resize(width, height);

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Save level to the filesystem.
    /// </summary>
    /// <param name="parentDirectory">The directory that will contain level/</param>
    /// /// <exception cref="DirectoryNotFoundException"></exception>
    public void Save(string parentDirectory)
    {
        if (!Path.Exists(parentDirectory)) 
            throw new DirectoryNotFoundException();

        if (string.IsNullOrEmpty(Name))
            throw new LevelParseException("Level name must not be null or empty");

        var targetDir = Path.Combine(parentDirectory, Name);

        System.IO.Directory.CreateDirectory(targetDir);

        var model = new IniData();

        model.Sections.AddSection("level");
        model["level"].AddKey("name", Name);
        model["level"].AddKey("width", $"{Width}");
        model["level"].AddKey("height", $"{Height}");
        model["level"].AddKey("depth", $"{Depth}");

        var parser = new FileIniDataParser();

        parser.WriteFile(Path.Combine(targetDir, "level.ini"), model);
    }

    /// <exception cref="LevelParseException"></exception>
    public static Level FromFile(string file)
    {
        if (!File.Exists(file)) 
            throw new LevelParseException("File 'level.ini' not found");
        
        var parser = new FileIniDataParser();

        var data = parser.ReadFile(file)["level"];

        var name = data["name"];
        var directory = Path.Combine(file, "..");
        var width = data["width"]?.ToInt() ?? DefaultWidth;
        var height = data["height"]?.ToInt() ?? DefaultHeight;
        var depth = data["depth"]?.ToInt() ?? 3;

        return new Level
        {
            Name = name,
            Directory = directory,
            Width = width,
            Height = height,
            Depth = depth,

            Geos = new(width, height, depth),
            Tiles = new(width, height, depth)
        };
    }
}