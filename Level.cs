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
using System.Text;

namespace Tiler.Editor;

public class Level
{
    public const int DefaultWidth = 70;
    public const int DefaultHeight = 40;
    public const int DefaultDepth = 5;

    public string? Name = "New Level";
    public string? Directory;
    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;
    public int Depth { get; private set; } = DefaultDepth;

    /// TODO: Use Either<> instead 
    public TileDef? DefaultTile { get; set; }
    public Managed.Image Lightmap = new(
        Raylib.GenImageColor(
            DefaultWidth + Viewports.LightmapMargin*2, 
            DefaultHeight + Viewports.LightmapMargin*2, 
            new Color(0,0,0,0)
        )
    );

    public float LightDistance { get; set; } = 0.3f;
    public int LightDirection { get; set; } = 90;

    public Matrix<Geo> Geos = new(DefaultWidth, DefaultHeight, DefaultDepth);
    public Matrix<ConnectionType> Connections = new(DefaultWidth, DefaultHeight, 1);
    public Matrix<TileDef?> Tiles = new(DefaultWidth, DefaultHeight, DefaultDepth);
    public List<LevelCamera> Cameras = [ new LevelCamera(new Vector2(20, 20)) ];
    public List<Effect> Effects = [];
    public List<Prop> Props = [];

    public Level() {}
    public Level(int width, int height)
    {
        Geos = new(width, height, DefaultDepth);
        Connections = new(width, height, 1);
        Tiles = new(width, height, DefaultDepth);

        Lightmap = new(
            Raylib.GenImageColor(
                width + Viewports.LightmapMargin*2, 
                height + Viewports.LightmapMargin*2, 
                new Color(0,0,0,0)
            )
        );
    
        Width = width;
        Height = height;
    }

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
        if (DefaultTile is not null) levelSec.AddKey("default_tile", DefaultTile.ID);

        model.Sections.AddSection("light");
        var lightSec = model["light"];

        lightSec.AddKey("distance", $"{LightDistance}");
        lightSec.AddKey("direction", $"{LightDirection}");

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
                        writer.Write(Geos[x, y, z] is Geo.Air ? "" : Geos[x, y, z]);

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

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    writer.Write(Connections[x, y, 0] is ConnectionType.None ? "" : Connections[x, y, 0]);

                    if (x < Width - 1) writer.Write('|');
                }

                if (y < Height - 1) writer.Write('|');
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

        var effectsTask = Task.Run(() =>
        {
            var effectsDir = Path.Combine(targetDir, "effects");

            if (!System.IO.Directory.Exists(effectsDir))
                System.IO.Directory.CreateDirectory(effectsDir);
            else foreach (var propFile in System.IO.Directory.GetFiles(effectsDir).Where(f => f.EndsWith(".ini"))) 
                File.Delete(propFile);

            for (var e = 0; e < Effects.Count; e++)
            {
                var effect = Effects[e];

                var stringified = new StringBuilder();

                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        if(effect.Matrix[x, y, 0] != 0.0f) stringified.Append($"{effect.Matrix[x, y, 0]:F02}");

                        if (x < Width - 1) stringified.Append('|');
                    }

                    if (y < Height - 1) stringified.Append('|');
                }

                File.WriteAllText(
                    Path.Combine(effectsDir, $"{e}.ini"), 
                    @$"[effect]
id = {effect.Def.ID}

[options]
{string.Join(Environment.NewLine, effect.Def.Config.Select((c, index) => $"{c.name} = {c.options[effect.OptionIndices[index]]}"))}

[data]
matrix = {stringified}
"
                );
            }
        });

        var propsTask = Task.Run(() =>
        {
            var propsDir = Path.Combine(targetDir, "props");

            if (!System.IO.Directory.Exists(propsDir))
                System.IO.Directory.CreateDirectory(propsDir);
            else foreach (var propFile in System.IO.Directory.GetFiles(propsDir).Where(f => f.EndsWith(".ini"))) 
                File.Delete(propFile);

            for (var p = 0; p < Props.Count; p++)
            {
                var prop = Props[p];

                File.WriteAllText(
                    Path.Combine(propsDir, $"{p}.ini"), 
                    @$"[prop]
id = {prop.Def.ID}
depth = {prop.Depth}
quad = {prop.Quad.TopLeft.X}/{prop.Quad.TopLeft.Y}|{prop.Quad.TopRight.X}/{prop.Quad.TopRight.Y}|{prop.Quad.BottomRight.X}/{prop.Quad.BottomRight.Y}|{prop.Quad.BottomLeft.X}/{prop.Quad.BottomLeft.Y}

[settings]
"
                );
            }
        });

        Raylib.ExportImage(Lightmap, Path.Combine(targetDir, "lightmap.png"));

        Task.WaitAll(geosTask, connectionsTask, tilesTask, camerasTask, effectsTask, propsTask);

        Log.Information("Level saved successfully");
    }

    /// TODO: Turn those into extensions

    /// <exception cref="LevelParseException"></exception>
    public static Level FromDir(string dir, TileDex tiles, PropDex props, EffectDex effects)
    {
        var iniFile = Path.Combine(dir, "level.ini");

        if (!File.Exists(iniFile)) 
            throw new LevelParseException("File 'level.ini' not found");
        
        var parser = new FileIniDataParser();

        var data = parser.ReadFile(iniFile);

        var levelSec = data["level"];

        var name = levelSec["name"];
        var width = levelSec["width"]?.ToInt() ?? DefaultWidth;
        var height = levelSec["height"]?.ToInt() ?? DefaultHeight;

        var def_tile_name = levelSec["default_tile"];

        if (!tiles.Tiles.TryGetValue(def_tile_name ?? "", out TileDef? defaultTile) && def_tile_name is not (null or ""))
            Log.Warning("Default tile '{TILE}' not found", def_tile_name);

        var lightSec = data["light"];

        var lightDistance = Math.Clamp(lightSec["distance"]?.ToFloat() ?? 0.3f, 0, 1f);
        var lightDirection = Math.Clamp(lightSec["direction"]?.ToInt() ?? 90, 0, 360);

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
                        c is "" ? Geo.Air : Enum.TryParse<Geo>(c, out var cell) ? cell : Geo.Solid,
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
                        c is "" ? ConnectionType.None : Enum.TryParse<ConnectionType>(c, out var cell) ? cell : ConnectionType.None,
                        i % width,                          // x
                        (i % (width * height)) / width,     // y
                        0                                   // z
                    )
                );

            foreach (var (cell, x, y, z) in cells) matrix[x, y, 0] = cell;

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

        var effectsTask = Task.Run(() =>
        {
            var effectsDir = Path.Combine(dir, "effects");

            if (!System.IO.Directory.Exists(effectsDir))
                return [];

            var propParser = new FileIniDataParser();

            var files = System.IO.Directory
                .GetFiles(effectsDir)
                .Where(d => d.EndsWith(".ini") && int.TryParse(Path.GetFileNameWithoutExtension(d), out _))
                .OrderBy(d => int.Parse(Path.GetFileNameWithoutExtension(d)));

            List<Effect> parsedEffects = [];
        
            foreach (var file in files)
            {
                var ini = propParser.ReadFile(file);

                var effectIni = ini["effect"];

                var id = effectIni["id"] ?? throw new EffectParseException("Missing field 'id'");

                if (!effects.Effects.TryGetValue(id, out var def))
                    throw new EffectParseException($"Undefined ID '{id}'");

                //

                Effect.TargetLayers targetLayers = 0;

                if (effectIni["layers"]?.Split(',') is string[] layers)
                {
                    if (layers is []) targetLayers = (Effect.TargetLayers)0b00011111;
                    else
                    {
                        foreach (var layerStr in layers)
                        {
                            if (!int.TryParse(layerStr, out var layer) || layer is < 1 or > 5)
                                throw new EffectParseException($"Invalid layers value");

                            targetLayers |= (Effect.TargetLayers)(2 << layer);
                        }
                    }
                }
                else
                {
                    targetLayers = (Effect.TargetLayers)0b00011111;
                }

                //

                var configIni = ini["options"];

                var options = new int[def.Config.Length];

                for (var c = 0; c < def.Config.Length; c++)
                {
                    var config = def.Config[c];

                    var value = configIni[config.name];

                    var index = Array.IndexOf(config.options, value);

                    if (index < 0)
                        throw new ParseException($"Invalid effect option '{config.name}' value '{value}'");

                    options[c] = index;
                }

                //

                var dataStr = ini["data"]?["matrix"]
                    ?? throw new EffectParseException("Missing 'matrix' field in [data] section");

                var cells = dataStr.Split('|')
                .Select((c, i) => 
                    (
                        c is "" ? 0 : Math.Clamp(float.TryParse(c, out var cell) ? cell : 0, 0, 1),
                        i % width,                          // x
                        (i % (width * height)) / width,     // y
                        0                // z
                    )
                );

                var effect = new Effect(def, width, height) { Layers = targetLayers, OptionIndices = options };

                foreach (var (cell, x, y, z) in cells) effect.Matrix[x, y, 0] = cell;

                parsedEffects.Add(effect);
            }

            return parsedEffects;
        });

        var propsTask = Task.Run(() =>
        {
            var propsDir = Path.Combine(dir, "props");

            if (!System.IO.Directory.Exists(propsDir))
                return [];

            var propParser = new FileIniDataParser();

            var files = System.IO.Directory
                .GetFiles(propsDir)
                .Where(d => d.EndsWith(".ini") && int.TryParse(Path.GetFileNameWithoutExtension(d), out _))
                .OrderBy(d => int.Parse(Path.GetFileNameWithoutExtension(d)));

            List<Prop> parsedProps = [];
        
            foreach (var file in files)
            {
                try
                {
                    var ini = propParser.ReadFile(file);

                    var propIni = ini["prop"];

                    var id = propIni["id"] ?? throw new PropParseException("Missing field 'id'");
                    
                    if (!props.Props.TryGetValue(id, out var def))
                        throw new PropParseException($"Undefined ID '{id}'");

                    var depth = propIni["depth"]?.ToInt() ?? 0;

                    var quadStr = propIni["quad"] ?? throw new PropParseException("Missing field 'quad'");

                    var quadVertices = quadStr.Split('|');

                    if (quadVertices is not [ string topLeft, string topRight, string bottomRight, string bottomLeft ])
                        throw new PropParseException("Invalid field value 'quad'");

                    var quad = new Quad();

                    try
                    {
                        quad.TopLeft = topLeft.ToVector2();
                        quad.TopRight = topRight.ToVector2();
                        quad.BottomRight = bottomRight.ToVector2();
                        quad.BottomLeft = bottomLeft.ToVector2();
                    }
                    catch (Exception qe)
                    {
                        throw new PropParseException("Invalid 'quad' value", qe);
                    }

                    var configIni = ini["settings"];

                    /// TODO: Complete here

                    parsedProps.Add(new Prop(def, config: def.CreateConfig(), quad, depth));
                }
                catch (Exception pe)
                {
                    throw new PropParseException($"Failed to load prop file '{Path.GetFileName(file)}'", pe);
                }
            }

            return parsedProps;
        });

        Task.WaitAll(geosTask, connectionsTask, tilesTask, camerasTask, effectsTask, propsTask);

        if (geosTask.IsFaulted)
            throw new ParseException("Failed to load geometry", geosTask.Exception);
        
        if (connectionsTask.IsFaulted)
            throw new ParseException("Failed to load connections", connectionsTask.Exception);

        if (tilesTask.IsFaulted)
            throw new ParseException("Failed to load tiles", tilesTask.Exception);

        if (camerasTask.IsFaulted)
            throw new ParseException("Failed to load cameras", camerasTask.Exception);

        if (effectsTask.IsFaulted)
            throw new ParseException("Failed to load effects", effectsTask.Exception);

        if (propsTask.IsFaulted)
            throw new ParseException("Failed to load props", propsTask.Exception);

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
            Cameras = camerasTask.Result,
            Effects = effectsTask.Result,
            Props = propsTask.Result
        };
    }
}