namespace Tiler.Editor.Rendering.Scripting;

using System;
using System.IO;
using System.Numerics;
using NLua;
using Raylib_cs;
using Serilog;
using Tiler.Editor.Tile;

public class TileRenderingScriptRuntime : IDisposable
{
    private readonly Lua lua;

    public TileDef Tile { get; init; }
    public string File { get; init; }
    private readonly Level level;
    private readonly LuaFunction renderFunc;
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

    public Vector2 CreatePoint(params object[] args) => args switch
    {
        [] => new(),
        [Vector2 vector] => new(vector.X, vector.Y),
        [float x, float y] => new(x, y),
        [int x, int y] => new(x, y),
        _ => throw new ScriptingException("Invalid arguments"),
    };

    public Rectangle CreateRect(params object[] args) => args switch
    {
        [] => new(),
        [Rectangle rect] => new(rect.Position, rect.Size),
        [float x, float y, float width, float height] => new(x, y, width, height),
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

    public TileRenderingScriptRuntime(
        TileDef tile, 
        string file, 
        Level level,
        Managed.RenderTexture[] layers
    ) {
        Tile = tile;
        File = file;
        this.layers = layers; 

        this.level = level;

        lua = new Lua();

        lua.DoString(
            "package.path = package.path .. \";" 
            + Directory.GetParent(file) + "/?.lua;" 
            + Directory.GetParent(file) + "/?/?.lua\""
        );

        lua.RegisterFunction(
            "print", 
            this, 
            typeof(TileRenderingScriptRuntime).GetMethod("Print")
        );

        lua.RegisterFunction(
            "debug", 
            this, 
            typeof(TileRenderingScriptRuntime).GetMethod("DebugPrint")
        );

        lua.RegisterFunction(
            "error", 
            this, 
            typeof(TileRenderingScriptRuntime).GetMethod("ErrorPrint")
        );

        lua.RegisterFunction(
            "Image",
            this,
            typeof(TileRenderingScriptRuntime).GetMethod("CreateImage")
        );

        lua.RegisterFunction(
            "Point",
            this,
            typeof(TileRenderingScriptRuntime).GetMethod("CreatePoint")
        );

        lua.RegisterFunction(
            "Rect",
            this,
            typeof(TileRenderingScriptRuntime).GetMethod("CreateRect")
        );

        lua.RegisterFunction(
            "Quad",
            this,
            typeof(TileRenderingScriptRuntime).GetMethod("CreateQuad")
        );

        lua.DoFile(file);
        
        var enumCount = 0;
        foreach (var name in Enum.GetNames(typeof(Geo))) lua[name] = enumCount++;

        lua["level"] = level;

        renderFunc = (LuaFunction) lua["Render"];
    }

    public void ExecuteRender(int x, int y, int z)
    {
        renderFunc.Call(x, y, z);
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

    ~TileRenderingScriptRuntime()
    {
        Dispose(false);
    }
}