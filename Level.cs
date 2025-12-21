using Tiler.Editor.Tile;

using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;

using IniParser;
using IniParser.Model;
using ImGuiNET;
using Raylib_cs;
using Serilog;
using System.Linq;
using System;
using Tiler.Editor.Managed;

namespace Tiler.Editor;

public class Level
{
    public const int DefaultWidth = 76;
    public const int DefaultHeight = 46;
    public const int DefaultDepth = 5;

    public string? Name = "New Level";
    public string? Directory;
    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;
    public int Depth { get; private set; } = DefaultDepth;

    /// TODO: Use Either<> instead 
    public TileDef? DefaultTile;
    public Managed.Image Lightmap = new(
        Raylib.GenImageColor(
            DefaultWidth + Viewports.LightmapMargin*2, 
            DefaultHeight + Viewports.LightmapMargin*2, 
            new Color(0,0,0,0)
        )
    );

    public int LightDistance { get; set; } = 1;
    public int LightDirection { get; set; } = 90;

    public Matrix<Geo> Geos = new(DefaultWidth, DefaultHeight, DefaultDepth);
    public Matrix<ConnectionType> Connections = new(DefaultWidth, DefaultHeight, 1);
    public Matrix<TileDef?> Tiles = new(DefaultWidth, DefaultHeight, DefaultDepth);
    public List<LevelCamera> Cameras = [ new LevelCamera(new Vector2(20, 20)) ];
    public List<Prop> Props = [];

    public void Resize(int width, int height)
    {
        Geos.Resize(width, height);
        Connections.Resize(width, height);
        Tiles.Resize(width, height);

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Save level to the filesystem.
    /// </summary>
    /// <param name="parentDirectory">The directory that will contain the level's directory (i.e projects/)</param>
    /// <param name="asName">Saves the level given a designated new name/</param>
    /// /// <exception cref="DirectoryNotFoundException"></exception>
    public void Save(string parentDirectory, string? asName = null)
    {
        if (!Path.Exists(parentDirectory)) 
            throw new DirectoryNotFoundException();

        if (string.IsNullOrEmpty(Name))
            throw new LevelParseException("Level name must not be null or empty");

        asName ??= Name;

        Log.Information($"Saving level as {asName}");

        var targetDir = Path.Combine(parentDirectory, asName);

        if (!System.IO.Directory.Exists(targetDir)) 
            System.IO.Directory.CreateDirectory(targetDir);

        var model = new IniData();

        model.Sections.AddSection("level");

        var levelSec = model["level"];

        levelSec.AddKey("name", asName);
        levelSec.AddKey("width", $"{Width}");
        levelSec.AddKey("height", $"{Height}");

        var parser = new FileIniDataParser();

        parser.WriteFile(Path.Combine(targetDir, "level.ini"), model);

        var geosTask = Task.Run(() =>
        {
            using var fs = new FileStream(Path.Combine(targetDir, "geometry.txt"), FileMode.Create);
            using var writer = new StreamWriter(fs);

            for (var z = 0; z < Depth; z++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        writer.Write(Geos[x, y, z]);

                        if (x < Width - 1) writer.Write('|');
                    }

                    if (y < Height - 1) writer.Write('|');
                }

                if (z < Depth - 1) writer.Write('|');
            }
        });
        
        var connectionsTask = Task.Run(() =>
        {
            using var fs = new FileStream(Path.Combine(targetDir, "connections.txt"), FileMode.Create);
            using var writer = new StreamWriter(fs);

            for (var z = 0; z < Depth; z++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        writer.Write(Connections[x, y, z]);

                        if (x < Width - 1) writer.Write('|');
                    }

                    if (y < Height - 1) writer.Write('|');
                }

                if (z < Depth - 1) writer.Write('|');
            }
        });

        var tilesTask = Task.Run(() =>
        {
            using var fs = new FileStream(Path.Combine(targetDir, "tiles.txt"), FileMode.Create);
            using var writer = new StreamWriter(fs);

            for (var z = 0; z < Depth; z++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        writer.Write(Tiles[x, y, z]?.ID ?? "");

                        if (x < Width - 1) writer.Write('|');
                    }

                    if (y < Height - 1) writer.Write('|');
                }

                if (z < Depth - 1) writer.Write('|');
            }
        });

        var camerasTask = Task.Run(() =>
        {
            // This is to ensure fractions are written in only one way.
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            using var fs = new FileStream(Path.Combine(targetDir, "cameras.txt"), FileMode.Create);
            using var writer = new StreamWriter(fs);

            foreach (var camera in Cameras)
            {
                writer.Write(camera.Position.X);
                writer.Write('/');
                writer.Write(camera.Position.Y);
                writer.Write('/');

                writer.Write(camera.TopLeft.Distance);
                writer.Write('/');
                writer.Write(camera.TopLeft.Angle);
                writer.Write('/');

                writer.Write(camera.TopRight.Distance);
                writer.Write('/');
                writer.Write(camera.TopRight.Angle);
                writer.Write('/');

                writer.Write(camera.BottomRight.Distance);
                writer.Write('/');
                writer.Write(camera.BottomRight.Angle);
                writer.Write('/');

                writer.Write(camera.BottomLeft.Distance);
                writer.Write('/');
                writer.Write(camera.BottomLeft.Angle);

                if (Cameras[^1] != camera) writer.Write('|');
            }
        });

        Raylib.ExportImage(Lightmap, Path.Combine(targetDir, "lightmap.png"));

        Task.WaitAll(geosTask, connectionsTask, tilesTask, camerasTask);

        Log.Information("Level saved successfully");
    }


    /// TODO: Turn those into extensions

    /// <exception cref="LevelParseException"></exception>
    public static Level FromDir(string dir, TileDex tiles)
    {
        var iniFile = Path.Combine(dir, "level.ini");

        if (!File.Exists(iniFile)) 
            throw new LevelParseException("File 'level.ini' not found");
        
        var parser = new FileIniDataParser();

        var data = parser.ReadFile(iniFile)["level"];

        var name = data["name"];
        var width = data["width"]?.ToInt() ?? DefaultWidth;
        var height = data["height"]?.ToInt() ?? DefaultHeight;
        var lightDistance = Math.Clamp(data["light_distance"]?.ToInt() ?? 1, 1, 10);
        var lightDirection = Math.Clamp(data["light_direction"]?.ToInt() ?? 90, 0, 360);
        _ = tiles.Tiles.TryGetValue(data["default_tile"] ?? "", out TileDef? defaultTile);

        var geosTask = Task.Run(() =>
        {
            var matrix = new Matrix<Geo>(width, height, DefaultDepth);

            var geosFile = Path.Combine(dir, "geometry.txt");

            if (!File.Exists(geosFile)) return matrix;

            var cells = File
                .ReadAllText(geosFile)
                .Split('|')
                .Select((c, i) => 
                    (
                        Enum.TryParse<Geo>(c, out var cell) ? cell : Geo.Solid,
                        i % width,                          // x
                        (i % (width * height)) / width,     // y
                        i / (width * height)                // z
                    )
                );

            foreach (var (cell, x, y, z) in cells) matrix[x, y, z] = cell;

            return matrix;
        });

        var connectionsTask = Task.Run(() =>
        {
            var matrix = new Matrix<ConnectionType>(width, height, 1);

            var connectionsFile = Path.Combine(dir, "connections.txt");

            if (!File.Exists(connectionsFile)) return matrix;

            var cells = File
                .ReadAllText(connectionsFile)
                .Split('|')
                .Select((c, i) => 
                    (
                        Enum.TryParse<ConnectionType>(c, out var cell) ? cell : ConnectionType.None,
                        i % width,                          // x
                        (i % (width * height)) / width,     // y
                        i / (width * height)                // z
                    )
                );

            foreach (var (cell, x, y, z) in cells) matrix[x, y, z] = cell;

            return matrix;
        });

        string[]? undefinedTiles = null;

        var tilesTask = Task.Run(() =>
        {
            var matrix = new Matrix<TileDef?>(width, height, DefaultDepth);

            var tilesFile = Path.Combine(dir, "tiles.txt");

            if (!File.Exists(tilesFile)) return matrix;

            var cellNames = File
                .ReadAllText(tilesFile)
                .Split('|')
                .Select((name, index) => (name, index))
                .Where(pair => pair.name != "" && pair.name != "\n")
                .GroupBy(pair => tiles.Tiles.ContainsKey(pair.name));

            undefinedTiles = [..cellNames.Where(group => !group.Key).SelectMany(g => g.Select(g => g.name)).Distinct()];

            var cells = cellNames
                .Where(n => n.Key)
                .SelectMany(defined => defined.Select((pair) => (
                        (
                            pair.name is "" ? null : tiles.Tiles[pair.name],
                            pair.index % width,                          // x
                            (pair.index % (width * height)) / width,     // y
                            pair.index / (width * height)                // z
                        )
                    )) 
                );

            foreach (var (cell, x, y, z) in cells) matrix[x, y, z] = cell;

            return matrix;
        });

        var camerasTask = Task.Run(() =>
        {
            List<LevelCamera> cameras = [];

            var camerasFile = Path.Combine(dir, "cameras.txt");

            if (!File.Exists(camerasFile)) return cameras;

            try {
                cameras = File
                    .ReadAllText(camerasFile)
                    .Split('|')
                    .Select((cameraText, i) =>
                    {
                        var fields = cameraText.Split('/');

                        if (
                            fields is not [ 
                                string posXStr, 
                                string posYStr, 
                                
                                string tldStr,
                                string tlaStr,
                                
                                string trdStr,
                                string traStr,
                                
                                string brdStr,
                                string braStr,
                                
                                string bldStr,
                                string blaStr,
                            ]
                        ) throw new ParseException($"Failed to parse camera #{i+1}: Missing camera fields");

                        if (!float.TryParse(posXStr, out var x)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid position X value '{posXStr}'");
                        
                        if (!float.TryParse(posYStr, out var y)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid position Y value '{posYStr}'");
                        
                        if (!float.TryParse(tldStr, out var tld)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid TL distance value '{tldStr}'");
                        
                        if (!int.TryParse(tlaStr, out var tla)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid TL angle value '{tlaStr}'");
                        
                        if (!float.TryParse(trdStr, out var trd)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid TR distance value '{trdStr}'");
                        
                        if (!int.TryParse(traStr, out var tra)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid TR angle value '{traStr}'");
                        
                        if (!float.TryParse(brdStr, out var brd)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid BR distance value '{brdStr}'");
                        
                        if (!int.TryParse(braStr, out var bra)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid BR angle value '{braStr}'");
                        
                        if (!float.TryParse(bldStr, out var bld)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid BL distance value '{bldStr}'");
                        
                        if (!int.TryParse(blaStr, out var bla)) 
                            throw new ParseException($"Failed to parse camera #{i+1}: Invalid BL angle value '{blaStr}'");

                        
                        return new LevelCamera
                        {
                            Position = new Vector2(x, y),
                            TopLeft = new LevelCameraVertex(tld, tla),
                            TopRight = new LevelCameraVertex(trd, tra),
                            BottomRight = new LevelCameraVertex(brd, bra),
                            BottomLeft = new LevelCameraVertex(bld, bla)
                        };
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                throw new ParseException("Failed to parse cameras", e);
            }

            return cameras;
        });

        Task.WaitAll(geosTask, connectionsTask, tilesTask, camerasTask);

        if (geosTask.IsFaulted)
            throw new ParseException("Failed to load geometry", geosTask.Exception);
        
        if (connectionsTask.IsFaulted)
            throw new ParseException("Failed to load connections", connectionsTask.Exception);

        if (tilesTask.IsFaulted)
            throw new ParseException("Failed to load tiles", tilesTask.Exception);

        if (camerasTask.IsFaulted)
            throw new ParseException("Failed to load cameras", camerasTask.Exception);

        if (undefinedTiles is not null)
            foreach (var und in undefinedTiles) Log.Warning("Undefined tile '{Name}'", und);

        using var lightmapRT = new RenderTexture(
                width * 20 + Viewports.LightmapMargin*2, 
                height * 20 + Viewports.LightmapMargin*2,
                new Color4(0,0,0,0),
                true
            );

        var lightmapFile = Path.Combine(dir, "lightmap.png");
        if (File.Exists(lightmapFile))
        {
            using var lightmapTexture = new Texture(Raylib.LoadTexture(lightmapFile));

            Raylib.BeginTextureMode(lightmapRT);
            Raylib.DrawTexture(lightmapTexture, 0, 0, Color.White);
            Raylib.EndTextureMode();
        }

        return new Level
        {
            Name = name,
            Directory = dir,
            Width = width,
            Height = height,
            DefaultTile = defaultTile,
            LightDistance = lightDistance,
            LightDirection = lightDirection,
            Lightmap = new Managed.Image(Raylib.LoadImageFromTexture(lightmapRT.Texture)),

            Geos = geosTask.Result,
            Connections = connectionsTask.Result,
            Tiles = tilesTask.Result,
            Cameras = camerasTask.Result
        };
    }
}