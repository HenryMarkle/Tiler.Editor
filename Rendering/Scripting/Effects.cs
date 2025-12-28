namespace Tiler.Editor.Rendering.Scripting;

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using NLua;
using Raylib_cs;
using Serilog;
using Tiler.Editor.Managed;
using Tiler.Editor.Tile;

public class EffectRenderingScriptRuntime : IDisposable
{
    private readonly Lua lua;

    public Effect Effect { get; init; }
    public string File { get; init; }
    private readonly Level level;
    private readonly int layerMargin;
    private readonly LuaFunction renderFunc;
    private readonly LevelCamera camera;
    private readonly Managed.RenderTexture[] layers;

    public void Print(params object[] args)
    {
        foreach (var a in args)
            Log.Information("[Script] {Out}", a.ToString());
    }

    public void DebugPrint(params object[] args)
    {
        foreach (var a in args)
            Log.Debug("[Script] {Out}", a.ToString());
    }

    public void ErrorPrint(params object[] args)
    {
        foreach (var a in args)
            Log.Error("[Script] {Out}", a.ToString());
    }

    public Managed.Texture? CreateImage(params object[] args)
    {
        if (args is [ string path ])
        {
            path = Path.Combine(Directory.GetParent(File)!.FullName, path);

            if (!System.IO.File.Exists(path)) return null;
            
            var texture = Raylib.LoadTexture(path);
            return new(texture);
        }
        else throw new ScriptingException("First argument expected to be a string");
    }
    
    public Managed.RenderTexture? CreateRenderTexture(params object[] args)
    {
        var (width, height) = args switch
        {
            [ long w, long h ] => ((int)w, (int)h),
            [ double w, double h ] => ((int)w, (int)h),
            [ var w, var h ] => (Convert.ToInt32(w), Convert.ToInt32(h)),
            _ => throw new ScriptingException("Invalid arguments")
        };

        return new RenderTexture(width, height, clearColor: new Color4(0, 0, 0, 0), clear: true);
    }

    public Vector2 CreatePoint(params object[] args) => args switch
    {
        [] => new(),
        [Vector2 vector] => new(vector.X, vector.Y),
        [long x, long y] => new(x, y),
        [double x, double y] => new((float)x, (float)y),
        [ var x, var y] => new((float)x, (float)y),
        _ => throw new ScriptingException("Invalid arguments"),
    };

    public Rectangle CreateRect(params object[] args) => args switch
    {
        [] => new(),
        [Rectangle rect] => new(rect.Position, rect.Size),
        [long x, long y, long width, long height] => new(x, y, width, height),
        [double x, double y, double width, double height] => new((float)x, (float)y, (float)width, (float)height),
        [var x, var y, var width, var height] => new(
            Convert.ToSingle(x), 
            Convert.ToSingle(y), 
            Convert.ToSingle(width), 
            Convert.ToSingle(height)
        ),
        _ => throw new ScriptingException("Invalid arguments"),
    };
    
    public Quad CreateQuad(params object[] args) => args switch
    {
        [] => new(),
        [Rectangle rect] => new(rect),
        [Quad quad] => new(quad.TopLeft, quad.TopRight, quad.BottomRight, quad.BottomLeft),
        [Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft] => new(topLeft, topRight, bottomRight, bottomLeft),
        _ => throw new ScriptingException("Invalid arguments"),
    };
    
    public Color4 CreateColor(params object[] args) => args switch
    {
        [] => new(),
        [Color4 color] => new(color.R, color.G, color.B, color.A),
        [long r, long g, long b] => new((byte)r, (byte)g, (byte)b, 255),
        [long r, long g, long b, long a] => new((byte)r, (byte)g, (byte)b, (byte)a),
        _ => throw new ScriptingException("Invalid arguments"),
    };

    public void DrawTexture(params object[] args)
    {
        switch (args)
        {
            case [ Texture texture, long layer, Rectangle destination, Rectangle source, .. ]:
            {        
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   layers[layer],
                    texture,
                    source,
                    destination with { 
                        X = destination.X + layerMargin - camera.Position.X, 
                        Y = destination.Y + layerMargin - camera.Position.Y 
                    },
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ Texture texture, long layer, Quad destination, Rectangle source, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   layers[layer],
                    texture, 
                    source, 
                    destination + (Vector2.One * layerMargin) - camera.Position, 
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;
            
            case [ Texture texture, long layer, Rectangle destination, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:       layers[layer],
                    texture,
                    source:   new(0, 0, texture.Width, texture.Height),
                    destination with { 
                        X = destination.X + layerMargin - camera.Position.X, 
                        Y = destination.Y + layerMargin - camera.Position.Y 
                    },
                    tint:     args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ Texture texture, long layer, Quad destination, .. ]:
            {
                    
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    layers[layer],
                    texture, 
                    source: new(0, 0, texture.Width, texture.Height), 
                    destination + (Vector2.One * layerMargin) - camera.Position,
                    tint:   args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ Texture2D texture, long layer, Rectangle destination, Rectangle source, .. ]:
            {        
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   layers[layer],
                    texture,
                    source,
                    destination with { 
                        X = destination.X + layerMargin - camera.Position.X, 
                        Y = destination.Y + layerMargin - camera.Position.Y 
                    },
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ Texture2D texture, long layer, Quad destination, Rectangle source, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   layers[layer],
                    texture, 
                    source, 
                    destination + (Vector2.One * layerMargin) - camera.Position, 
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;
            
            case [ Texture2D texture, long layer, Rectangle destination, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:       layers[layer],
                    texture,
                    source:   new(0, 0, texture.Width, texture.Height),
                    destination with { 
                        X = destination.X + layerMargin - camera.Position.X, 
                        Y = destination.Y + layerMargin - camera.Position.Y 
                    },
                    tint:     args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ Texture2D texture, long layer, Quad destination, .. ]:
            {
                    
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    layers[layer],
                    texture, 
                    source: new(0, 0, texture.Width, texture.Height), 
                    destination + (Vector2.One * layerMargin) - camera.Position,
                    tint:   args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            default: throw new ScriptingException("Invalid arguments");
        }
    }

    public void DrawTextureRT(params object[] args)
    {
        switch (args)
        {
            case [ RenderTexture rt, Texture texture, Rectangle destination, Rectangle source, .. ]:
            {        
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   rt,
                    texture,
                    source,
                    destination,
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ RenderTexture rt, Texture texture, Quad destination, Rectangle source, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:   rt,
                    texture, 
                    source, 
                    destination, 
                    tint: args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;
            
            case [ RenderTexture rt, Texture texture, Rectangle destination, .. ]:
            {
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt:       rt,
                    texture,
                    source:   new(0, 0, texture.Width, texture.Height),
                    destination,
                    tint:     args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            case [ RenderTexture rt, Texture texture, Quad destination, .. ]:
            {
                    
                // Raylib.BeginTextureMode(layers[layer]);
                RlUtils.DrawTextureRT(
                    rt,
                    texture, 
                    source: new(0, 0, texture.Width, texture.Height), 
                    destination,
                    tint:   args.Last() is Color4 c ? c : Color.White
                );
                // Raylib.EndTextureMode();
            }
            break;

            default: throw new ScriptingException("Invalid arguments");
        }
    }

    public EffectRenderingScriptRuntime(
        Effect effect, 
        Level level,
        LevelCamera camera,
        Managed.RenderTexture[] layers,
        int layerMargin
    ) {
        Effect = effect;
        File = Path.Combine(effect.Def.ResourceDir, "script.lua");
        this.camera = camera;
        this.layers = layers; 
        this.layerMargin = layerMargin;

        this.level = level;

        lua = new Lua();

        lua.DoString(
            "package.path = package.path .. \";" 
            + Directory.GetParent(File)!.FullName + "/?.lua;" 
            + Directory.GetParent(File)!.FullName + "/?/?.lua;"
            + Path.GetFullPath(
                Path.Combine(Directory.GetParent(File)!.FullName, "..", "..", "scripts")
              ) + "/?.lua\""
        );

        lua.RegisterFunction(
            "print", 
            this, 
            typeof(EffectRenderingScriptRuntime).GetMethod("Print")
        );

        lua.RegisterFunction(
            "debug", 
            this, 
            typeof(EffectRenderingScriptRuntime).GetMethod("DebugPrint")
        );

        lua.RegisterFunction(
            "error", 
            this, 
            typeof(EffectRenderingScriptRuntime).GetMethod("ErrorPrint")
        );

        lua.RegisterFunction(
            "Image",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateImage")
        );

        lua.RegisterFunction(
            "RenderTexture",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateRenderTexture")
        );

        lua.RegisterFunction(
            "Point",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreatePoint")
        );

        lua.RegisterFunction(
            "Rect",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateRect")
        );

        lua.RegisterFunction(
            "Quad",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateQuad")
        );

        lua.RegisterFunction(
            "Draw",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("DrawTexture")
        );

        lua.RegisterFunction(
            "DrawOn",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("DrawTextureRT")
        );

        lua.RegisterFunction(
            "Color",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateColor")
        );

        foreach (var name in Enum.GetNames(typeof(Geo))) lua[name] = name;

        lua["White"] = new Color4(255, 255, 255);
        lua["Black"] = new Color4(0, 0, 0);
        lua["Red"] = new Color4(255, 0, 0);
        lua["Green"] = new Color4(0, 255, 0);
        lua["Blue"] = new Color4(0, 0, 255);
        lua["Purple"] = new Color4(255, 0, 255);
        lua["Gray"] = Color.Gray;

        lua["Camera"] = camera;
        lua["Margin"] = layerMargin;

        lua["StartX"] = (int)(camera.Position.X - layerMargin)/20;
        lua["StartY"] = (int)(camera.Position.Y - layerMargin)/20;

        lua["Columns"] = (1400 + layerMargin*2)/2;
        lua["Rows"] = (800 + layerMargin*2)/2;

        lua["Level"] = level;

        lua["Effect"] = Effect;

        lua.DoFile(File);

        renderFunc = (LuaFunction) lua["Render"];
    }

    public void ExecuteRender()
    {
        renderFunc.Call();
    }

    public void ExecuteRender(int row)
    {
        renderFunc.Call(row);
    }
    
    public void ExecuteRender(int x, int y)
    {
        renderFunc.Call(x, y);
    }

    public bool IsDisposed { get; private set; }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lua.Dispose();
        }
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        Dispose(true);

        GC.SuppressFinalize(this);

        IsDisposed = true;
    }

    ~EffectRenderingScriptRuntime()
    {
        Dispose(false);
    }
}