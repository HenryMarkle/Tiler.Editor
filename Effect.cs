using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using IniParser;

namespace Tiler.Editor;

public class EffectDef(string id, string resourceDir)
{
    public enum RenderProcess
    {
        AllAtOnce,
        PerCell,
        PerRow
    }

    public string ID { get; init; } = id;
    public string ResourceDir { get; init; } = resourceDir;
    public RenderProcess Render { get; init; } = RenderProcess.AllAtOnce;

    public string? Name { get; init; }
    public string? Category { get; init; }

    public (string name, string[] options)[] Config { get; init; } = [];

    public static EffectDef FromDir(string dir)
    {
        var file = Path.Combine(dir, "effect.ini");

        if (!File.Exists(file))
            throw new EffectParseException("'effect.ini' not found");

        var parser = new FileIniDataParser();

        var ini = parser.ReadFile(file);
        var effIni = ini["effect"];

        var id = effIni["id"] ?? throw new EffectParseException("Required 'id' key");
        var name = effIni["name"];
        var category = effIni["category"];
        var processRes = Enum.TryParse<RenderProcess>(effIni["process"] ?? "AllAtOnce", out var process);

        return new EffectDef(id, dir)
        {
            Name = name,
            Category = category,
            Render = processRes ? process : RenderProcess.AllAtOnce
        };
    }

    public static bool operator==(EffectDef lhs, EffectDef? rhs) => lhs.Equals(rhs);
    public static bool operator!=(EffectDef lhs, EffectDef? rhs) => !lhs.Equals(rhs);

    public override bool Equals(object? obj) => obj is EffectDef effect && GetHashCode() == effect.GetHashCode();
    public override int GetHashCode() => ID.GetHashCode();

    public override string ToString() => $"EffectDef({ID})";
}

public class Effect
{
    public enum TargetLayers : byte
    {
        One   = 2 << 1,
        Two   = 2 << 2,
        Three = 2 << 3,
        Four  = 2 << 4,
        Five  = 2 << 5
    }

    public EffectDef Def { get; set; }

    /// <summary>
    /// A 2D matrix containing values from zero to one, denoting the strength of the effect
    /// </summary>
    public Matrix<float> Matrix { get; set; }
    public TargetLayers Layers { get; set; } = (TargetLayers)0b00011111; // All
    public int[] OptionIndices { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Targets(TargetLayers layer) => (layer & Layers) == layer;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Targets(int layer) => ((TargetLayers)layer & Layers) == (TargetLayers)layer;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Targets(params int[] layers)
    {
        TargetLayers targetLayers = 0;

        foreach (var l in layers) targetLayers |= (TargetLayers)l;

        return Targets(targetLayers);
    }

    public Effect(EffectDef def, int width, int height)
    {
        Def = def;
        Matrix = new Matrix<float>(width, height, 1);
        OptionIndices = new int[Def.Config.Length];
    }

    public Effect(Effect effect)
    {
        Def = effect.Def;
        Matrix = new Matrix<float>(effect.Matrix.Width, effect.Matrix.Height, 1);

        for (var y = 0; y < Matrix.Height; y++)
            for (var x = 0; x < Matrix.Width; x++)
                Matrix[x, y, 0] = effect.Matrix[x, y, 0];

        OptionIndices = [..effect.OptionIndices];
        Layers = effect.Layers;
    }
}