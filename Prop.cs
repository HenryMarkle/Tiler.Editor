using System;
using System.IO;
using System.Linq;
using IniParser;
using Raylib_cs;
using Serilog;

namespace Tiler.Editor;

public abstract class PropConfig
{
    public abstract PropConfig Clone();
}
public class VoxelStructConfig : PropConfig
{
    public override VoxelStructConfig Clone()
    {
        return new();
    }
}
public class SoftConfig : PropConfig
{
    public int Depth;

    public override SoftConfig Clone()
    {
        return new()
        {
            Depth = Depth
        };
    }
}
public class AntimatterConfig : PropConfig
{
    public int Depth;

    public override AntimatterConfig Clone()
    {
        return new()
        {
            Depth = Depth
        };
    }
}
public class CustomPropConfig : PropConfig
{
    public override CustomPropConfig Clone()
    {
        return new();
    }
}

//

public abstract class PropDef(string id, string resourceDir)
{
    public string ID { get; } = id;
    public string ResourceDir { get; set; } = resourceDir;

    public string? Name { get; set; }
    public string? Category { get; set; }

    public abstract PropConfig CreateConfig();

    /// <summary>
    /// Loads prop definition from the folder containing 'prop.ini'
    /// </summary>
    /// <param name="dir">The directory containing 'prop.ini'</param>
    public static PropDef FromDir(string dir)
    {
        if (!Directory.Exists(dir))
            throw new PropParseException("Directory not found");

        var iniFile = Path.Combine(dir, "prop.ini");

        if (!File.Exists(iniFile))
            throw new PropParseException("'prop.ini' not found");

        var parser = new FileIniDataParser();

        var data = parser.ReadFile(iniFile)["prop"];
    
        if (!data.ContainsKey("id"))
            throw new PropParseException("Required 'id' key");
        
        if (!data.ContainsKey("type"))
            throw new PropParseException("Required 'type' key");
        

        var id = data["id"];
        var type = data["type"];
        var name = data["name"];
        var category = data["category"];

        switch (type)
        {
            case "VoxelStruct":
            {
                var width = data["width"]?.ToInt();
                var height = data["height"]?.ToInt();
                var layers = data["layers"]?.ToInt();
                var repeat = data["repeat"]?.Split(',').Select(s => s.ToInt()).ToArray();

                var imagePath = Path.Combine(dir, "layers.png");
                if (!File.Exists(imagePath))
                    throw new PropParseException($"{type}'s 'layers.png' not found");

                var image = new Managed.Image(Raylib.LoadImage(imagePath));
                
                // Infere missing properties

                repeat ??= Enumerable.Range(0, layers ?? 1).Select(_ => 1).ToArray() ?? [ 1 ];
                layers ??= repeat.Length;
                width ??= image.Width;
                height ??= image.Height / layers;

                // Validate

                if (layers != repeat.Length)
                    throw new PropParseException($"{type} 'layers' does not match 'repeat' length");

                if (height != image.Height / layers)
                    throw new PropParseException($"{type}'s image height does not match 'height' value");

                return new VoxelStruct(id, dir)
                {
                    Name = name,
                    Category = category,
                    Width = width.Value,
                    Height = height.Value,
                    Layers = layers.Value,
                    Repeat = repeat,
                    Image = image
                };
            }

            case "Soft":
            {
                var depth = data["depth"]?.ToInt() ?? 10;

                var imagePath = Path.Combine(dir, "image.png");
                if (!File.Exists(imagePath))
                    throw new PropParseException($"{type}'s 'image.png' not found");

                var image = new Managed.Image(Raylib.LoadImage(imagePath));

                return new Soft(id, dir)
                {
                    Name = name,
                    Category = category,
                    DefaultDepth = depth,
                    Image = image
                };
            }

            case "Antimatter":
            {
                var depth = data["depth"]?.ToInt() ?? 10;

                var imagePath = Path.Combine(dir, "image.png");
                if (!File.Exists(imagePath))
                    throw new PropParseException($"{type}'s 'image.png' not found");

                var image = new Managed.Image(Raylib.LoadImage(imagePath));

                return new Antimatter(id, dir)
                {
                    Name = name,
                    Category = category,
                    DefaultDepth = depth,
                    Image = image
                };
            }

            case "Custom":
            {
                var imagePath = Path.Combine(dir, "preview.png");
                if (!File.Exists(imagePath))
                    throw new PropParseException($"{type}'s 'preview.png' not found");

                var image = new Managed.Image(Raylib.LoadImage(imagePath));

                var scriptPath = Path.Combine(dir, "script.lua");
                if (!File.Exists(scriptPath))
                    throw new PropParseException($"{type}'s 'script.lua' not found");

                return new Custom(id, dir)
                {
                    Name = name,
                    Category = category,
                    Image = image,
                    ScriptFile = scriptPath
                };
            }

            default: throw new PropParseException($"Unknown prop type '{type}'");
        }
    }

    public static bool operator==(PropDef lhs, PropDef? rhs) => lhs.Equals(rhs);
    public static bool operator!=(PropDef lhs, PropDef? rhs) => !lhs.Equals(rhs);

    public override bool Equals(object? obj) => obj is PropDef prop && GetHashCode() == prop.GetHashCode();
    public override int GetHashCode() => ID.GetHashCode();
    public override string ToString() => $"PropDef({ID})";
}

public class VoxelStruct(string id, string resourceDir) : PropDef(id, resourceDir)
{
    public int Width { get; init; } = 20;
    public int Height { get; init; } = 20;

    public int Layers { get; init; } = 1;
    public int[] Repeat { get; init; } = [ 1 ];

    public required Managed.Image Image { get; init; }

    public override VoxelStructConfig CreateConfig()
    {
        return new VoxelStructConfig();
    }

    public override string ToString() => $"VoxelStructProp({ID})";
}

public class Soft(string id, string resourceDir) : PropDef(id, resourceDir)
{
    public int Width => Image.Width;
    public int Height => Image.Height;
    public int DefaultDepth { get; init; } = 10;

    public required Managed.Image Image { get; init; }

    public override SoftConfig CreateConfig()
    {
        return new SoftConfig();
    }

    public override string ToString() => $"SoftProp({ID})";
}

public class Antimatter(string id, string resourceDir) : PropDef(id, resourceDir)
{
    public int Width => Image.Width;
    public int Height => Image.Height;
    public int DefaultDepth { get; init; } = 10;

    public required Managed.Image Image { get; init; }

    public override AntimatterConfig CreateConfig()
    {
        return new AntimatterConfig();
    }

    public override string ToString() => $"AntimatterProp({ID})";
}

public class Custom(string id, string resourceDir) : PropDef(id, resourceDir)
{
    public int Width => Image.Width;
    public int Height => Image.Height;
    public string ScriptFile { get; init; } = Path.Combine(resourceDir, "script.lua");

    public required Managed.Image Image { get; init; }

    public override CustomPropConfig CreateConfig()
    {
        return new CustomPropConfig();
    }

    public override string ToString() => $"CustomProp({ID})";
}

public class Prop
{
    private PropConfig _config;

    public required PropDef Def { get; init; }
    public required PropConfig Config
    {
        get => _config;
        set
        {
            _config = value;

            switch ((Def, value))
            {
                case (VoxelStruct, VoxelStructConfig): return;
                case (Soft, SoftConfig): return;
                case (Custom, CustomPropConfig): return;
                default: 
                    throw new TilerException(
                        $"Incompatible prop config type; definition type was {Def.GetType().Name}, but got {value.GetType().Name}"
                    );
            }
        }
    }
    public int Depth;
    public required Quad Quad;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Prop() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Prop(Prop prop)
    {
        Def = prop.Def;
        _config = prop.Config.Clone();
        Depth = prop.Depth;
        Quad = new Quad(prop.Quad);
    }

    public override string ToString() => $"Prop({Def}, {Depth}, {Quad})";
}