namespace Tiler.Editor.Rendering.Scripting;

using System;
using System.Collections.Generic;
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
        if (args is [string path])
        {
            path = Path.Combine(Directory.GetParent(File)!.FullName, path);

            if (!System.IO.File.Exists(path))
                throw new ScriptingException($"Image file not found '{path}'");

            var texture = Raylib.LoadTexture(path);
            return new(texture);
        }
        else if (args is [Texture2D texture])
        {
            var image = Raylib.LoadImageFromTexture(texture);
            var copy = Raylib.LoadTextureFromImage(image);

            Raylib.UnloadImage(image);

            return new(copy);
        }
        else if (args is [Texture mtexture])
        {
            var image = Raylib.LoadImageFromTexture(mtexture);
            var copy = Raylib.LoadTextureFromImage(image);

            Raylib.UnloadImage(image);

            return new(copy);
        }
        else if (args is [RenderTexture rt])
        {
            var image = Raylib.LoadImageFromTexture(rt.Texture);
            var copy = Raylib.LoadTextureFromImage(image);

            Raylib.UnloadImage(image);

            return new(copy);
        }
        else throw new ScriptingException("First argument expected to be a string");
    }

    public Managed.Shader? CreateShaderFromFiles(params object[] args)
    {
        switch (args)
        {
            case [string fragmentShaderPath]:
                {
                    var path = Path.Combine(Directory.GetParent(File)!.FullName, fragmentShaderPath);

                    if (!System.IO.File.Exists(path))
                        throw new ScriptingException($"Fragment shader file not found '{path}'");

                    return Managed.Shader.FromFiles(null, path);
                }

            case [string vertexShaderPath, string fragmentShaderPath]:
                {
                    var vpath = Path.Combine(Directory.GetParent(File)!.FullName, vertexShaderPath);
                    var fpath = Path.Combine(Directory.GetParent(File)!.FullName, fragmentShaderPath);

                    if (!System.IO.File.Exists(vpath))
                        throw new ScriptingException($"Vertex shader file not found '{vpath}'");

                    if (!System.IO.File.Exists(fpath))
                        throw new ScriptingException($"Fragment shader file not found '{fpath}'");

                    return Managed.Shader.FromFiles(vpath, fpath);
                }

            default: throw new ScriptingException("Invalid arguments");
        }
    }

    public Managed.RenderTexture? CreateRenderTexture(params object[] args)
    {
        var (width, height) = args switch
        {
            [long w, long h] => ((int)w, (int)h),
            [double w, double h] => ((int)w, (int)h),
            [var w, var h] => (Convert.ToInt32(w), Convert.ToInt32(h)),
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
        [var x, var y] => new((float)x, (float)y),
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
        if (args is not [ LuaTable argsTable ]) 
            throw new ScriptingException("Invalid arguments");

        var texture = argsTable["texture"] switch { 
            Texture t => t.Raw, 
            Texture2D t2 => t2, 
            _ => throw new ScriptingException("Invalid 'texture' argument type") 
        };

        var layer = (long)(argsTable["layer"] ?? 0);
        var src = (Rectangle)argsTable["source"];
        Color tint = argsTable["tint"] is Color4 c ? c : Color.White;
        var shader = (Managed.Shader?)argsTable["shader"];
        var alphaBlend = (bool)(argsTable["alphaBlend"] ?? true);

        var rt = layers[layer];

        if (argsTable["dest"] is Quad quad)
        {
            Raylib.BeginTextureMode(rt);
            if (shader is not null)
            {
                shader.Begin();
                if (argsTable["shaderValues"] is LuaTable values) shader.Set(values);
            }

            if (!alphaBlend)
            {
                Raylib.BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(1, 0, 1);
            }

            quad += + (Vector2.One * layerMargin) - camera.Position;
            
            quad = new Quad(
                topLeft:     new Vector2(quad.BottomLeft.X, rt.Texture.Height - quad.BottomLeft.Y),
                topRight:    new Vector2(quad.BottomRight.X, rt.Texture.Height - quad.BottomRight.Y),
                bottomRight: new Vector2(quad.TopRight.X, rt.Texture.Height - quad.TopRight.Y),
                bottomLeft:  new Vector2(quad.TopLeft.X, rt.Texture.Height - quad.TopLeft.Y)
            );

            RlUtils.DrawTextureQuad(
                texture, 
                source: src with { Y = src.Y + src.Height, Height = -src.Height }, 
                quad, 
                tint
            );

            shader?.End();
            if (!alphaBlend)
            {
                Raylib.EndBlendMode();
            }
            Raylib.EndTextureMode();
        }
        else if (argsTable["dest"] is Rectangle dest)
        {
            Raylib.BeginTextureMode(rt);
            if (shader is not null)
            {
                Raylib.BeginShaderMode(shader);
                // if (argsTable["shaderValues"] is LuaTable values) shader.Set(values);

                if (argsTable["shaderValues"] is LuaTable values) 
                    foreach (KeyValuePair<object, object> entry in values)
                    {
                        shader.Set(entry.Key as string, entry.Value);
                    }
            }
            if (!alphaBlend)
            {
                Raylib.BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(1, 0, 1);
            }

            dest = dest with { 
                X = dest.X + layerMargin - camera.Position.X, 
                Y = dest.Y + layerMargin - camera.Position.Y 
            }; 

            Raylib.DrawTexturePro(
                texture,
                source: src with { Height = -src.Height },
                dest: new Rectangle(
                    dest.X,
                    rt.Texture.Height - dest.Height - dest.Y,
                    dest.Width,
                    dest.Height
                ),
                origin: Vector2.Zero,
                rotation: 0,
                tint
            );

            if (shader is not null) Raylib.EndShaderMode();
            if (!alphaBlend)
            {
                Raylib.EndBlendMode();
            }
            Raylib.EndTextureMode();
        }
        else throw new ScriptingException("'dest' must be specified");
    }

    public void DrawTextureRT(params object[] args)
    {
        if (args is not [ LuaTable argsTable ]) 
            throw new ScriptingException("Invalid arguments");

        var rt = argsTable["rt"] switch { 
            RenderTexture t => t.Raw, 
            RenderTexture2D t2 => t2, 
            _ => throw new ScriptingException("Invalid 'rt' argument type") 
        };
        var texture = argsTable["texture"] switch { 
            Texture t => t.Raw, 
            Texture2D t2 => t2, 
            _ => throw new ScriptingException("Invalid 'texture' argument type") 
        };
        var src = (Rectangle)argsTable["source"];
        Color tint = argsTable["tint"] is Color4 c ? c : Color.White;
        var shader = (Managed.Shader?)argsTable["shader"];
        var alphaBlend = (bool)(argsTable["alphaBlend"] ?? true);

        if (argsTable["dest"] is Quad quad)
        {
            Raylib.BeginTextureMode(rt);
            if (shader is not null)
            {
                shader.Begin();
                if (argsTable["shaderValues"] is LuaTable values) shader.Set(values);
            }
            Raylib.BeginTextureMode(rt);
            if (shader is not null)
            {
                shader.Begin();
                if (argsTable["shaderValues"] is LuaTable values) shader.Set(values);
            }
            if (!alphaBlend)
            {
                Raylib.BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(1, 0, 1);
            }
            
            quad = new Quad(
                topLeft:     new Vector2(quad.BottomLeft.X, rt.Texture.Height - quad.BottomLeft.Y),
                topRight:    new Vector2(quad.BottomRight.X, rt.Texture.Height - quad.BottomRight.Y),
                bottomRight: new Vector2(quad.TopRight.X, rt.Texture.Height - quad.TopRight.Y),
                bottomLeft:  new Vector2(quad.TopLeft.X, rt.Texture.Height - quad.TopLeft.Y)
            );

            RlUtils.DrawTextureQuad(
                texture, 
                source: src with { Y = src.Y + src.Height, Height = -src.Height }, 
                quad, 
                tint
            );
            
            shader?.End();
            if (!alphaBlend)
            {
                Raylib.EndBlendMode();
            }
            Raylib.EndTextureMode();
        }
        else if (argsTable["dest"] is Rectangle dest)
        {
            Raylib.BeginTextureMode(rt);
            if (shader is not null)
            {
                shader.Begin();
                if (argsTable["shaderValues"] is LuaTable values) shader.Set(values);
            }
            if (!alphaBlend)
            {
                Raylib.BeginBlendMode(BlendMode.Custom);
                Rlgl.SetBlendFactors(1, 0, 1);
            }
            Raylib.DrawTexturePro(
                texture,
                source: src with { Height = -src.Height },
                dest: new Rectangle(
                    dest.X,
                    rt.Texture.Height - dest.Height - dest.Y,
                    dest.Width,
                    dest.Height
                ),
                origin: Vector2.Zero,
                rotation: 0,
                tint
            );
            shader?.End();
            if (!alphaBlend)
            {
                Raylib.EndBlendMode();
            }
            Raylib.EndTextureMode();
        }
        else throw new ScriptingException("'dest' must be specified");
    }

    public EffectRenderingScriptRuntime(
        Effect effect,
        Level level,
        LevelCamera camera,
        Managed.RenderTexture[] layers,
        int layerMargin
    )
    {
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
            "Shader",
            this,
            typeof(EffectRenderingScriptRuntime).GetMethod("CreateShaderFromFiles")
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

        foreach (var name in Enum.GetNames<Geo>()) lua[name] = name;

        lua["White"] = new Color4(255, 255, 255);
        lua["Black"] = new Color4(0, 0, 0);
        lua["Red"] = new Color4(255, 0, 0);
        lua["Green"] = new Color4(0, 255, 0);
        lua["Blue"] = new Color4(0, 0, 255);
        lua["Purple"] = new Color4(255, 0, 255);
        lua["Gray"] = Color.Gray;

        lua["Camera"] = camera;
        lua["Margin"] = layerMargin;
        lua["Layers"] = layers;

        lua["StartX"] = (int)(camera.Position.X - layerMargin) / 20;
        lua["StartY"] = (int)(camera.Position.Y - layerMargin) / 20;

        lua["Columns"] = (1400 + layerMargin * 2) / 2;
        lua["Rows"] = (800 + layerMargin * 2) / 2;

        lua["Level"] = level;

        lua["Effect"] = Effect;

        lua.DoFile(File);

        renderFunc = (LuaFunction)lua["Render"];
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