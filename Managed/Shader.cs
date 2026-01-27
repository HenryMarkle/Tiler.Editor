namespace Tiler.Editor.Managed;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using Tiler.Editor.Rendering.Scripting;

public class Shader(Raylib_cs.Shader shader) : IDisposable
{
    public Raylib_cs.Shader Raw = shader;


    public static implicit operator Raylib_cs.Shader(Shader s) => s.Raw;

    public static Shader FromFiles(string? vs, string fs) => new(Raylib.LoadShader(vs, fs));
    public static Shader FromMemory(string? vs, string fs) => new(Raylib.LoadShaderFromMemory(vs, fs));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(string uniformName, Texture texture) => Raylib.SetShaderValueTexture(Raw, Raylib.GetShaderLocation(Raw, uniformName), texture);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(string uniformName, Texture2D texture) => Raylib.SetShaderValueTexture(Raw, Raylib.GetShaderLocation(Raw, uniformName), texture);
    
    public void Set(string uniformName, object value)
    {
        var locIndex = Raylib.GetShaderLocation(Raw, uniformName);

        switch (value)
        {
            case int i:
            Raylib.SetShaderValue(Raw, locIndex, i, ShaderUniformDataType.Int);
            break;

            case long l:
            Raylib.SetShaderValue(Raw, locIndex, Convert.ToInt32(l), ShaderUniformDataType.Int);
            break;

            case float f:
            Raylib.SetShaderValue(Raw, locIndex, f, ShaderUniformDataType.Float);
            break;

            case double d:
            Raylib.SetShaderValue(Raw, locIndex, Convert.ToSingle(d), ShaderUniformDataType.Float);
            break;

            case Vector2 v:
            Raylib.SetShaderValueV(Raw, locIndex, [ v.X, v.Y ], ShaderUniformDataType.Vec2, 1);
            break;

            case Texture t:
            Raylib.SetShaderValueTexture(Raw, locIndex, t.Raw);
            break;

            case Texture2D t:
            Raylib.SetShaderValueTexture(Raw, locIndex, t);
            break;

            case NLua.LuaTable t:
                {
                    var values = new object[t.Values.Count];

                    t.Values.CopyTo(values, 0);

                    if (Array.TrueForAll(values, value => value is long))
                    {
                        int[] converted = [..values.Select(v => Convert.ToInt32(v))];
                        Raylib.SetShaderValueV(Raw, locIndex, converted, ShaderUniformDataType.Int, converted.Length);
                    }
                    else if (Array.TrueForAll(values, value => value is double)) {
                        float[] converted = [..values.Select(v => Convert.ToSingle(v))];
                        Raylib.SetShaderValueV(Raw, locIndex, converted, ShaderUniformDataType.Float, converted.Length);
                    }
                    else if (Array.TrueForAll(values, value => value is Vector2)) {
                        Vector2[] converted = [..values.Cast<Vector2>()];
                        Raylib.SetShaderValueV(Raw, locIndex, converted, ShaderUniformDataType.Vec2, converted.Length);
                    }
                    else throw new ScriptingException("Invalid shader values");
                }
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(NLua.LuaTable table)
    {
        foreach (KeyValuePair<object, object> entry in table)
            Set((entry.Key as string)!, entry.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Begin() => Raylib.BeginShaderMode(Raw);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void End() => Raylib.EndShaderMode();

    public bool IsDisposed { get; private set; }
    public void Dispose()
    {
        if (IsDisposed) return;
        Unloader.Enqueue(Raw);
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    ~Shader()
    {
        Dispose();
    }
}