using System.Collections.Generic;
using System.Linq;
using Tiler.Editor.Rendering.Scripting;
using Tiler.Editor.Tile;

namespace Tiler.Editor.Rendering;

public class EffectRenderer
{
    private const int Width = 1400;
    private const int Height = 800;

    private const int LayerMargin = 100;

    private const int Columns = (Width + LayerMargin*2) / 20;
    private const int Rows = (Height + LayerMargin*2) / 20;

    public int X { get; private set;}
    public int Y { get; private set; }
    public int EffectIndex { get; private set; }

    public Effect? CurrentEffect => 
        EffectIndex >= 0 && EffectIndex < level.Effects.Count 
            ? level.Effects[EffectIndex] 
            : null;

    private EffectRenderingScriptRuntime? scriptRuntime;

    private readonly Managed.RenderTexture[] layers;
    public readonly Level level;
    private readonly LevelCamera camera;

    public bool IsDone { get; private set; }


    public EffectRenderer(Managed.RenderTexture[] layers, Level level, LevelCamera camera)
    {
        this.layers = layers;
        this.level = level;
        this.camera = camera;
        IsDone = false;

        if (level.Effects.Count == 0)
        {
            IsDone = true;
            return;
        }

        X = (int)(camera.Position.X - LayerMargin)/20;
        Y = (int)(camera.Position.Y - LayerMargin)/20;

        scriptRuntime = new(level.Effects[0], level, camera, layers, LayerMargin);
    }

    public void Next()
    {
        if (EffectIndex >= level.Effects.Count)
        {
            IsDone = true;
            return;
        }

        switch (level.Effects[EffectIndex].Def.Render)
        {
            case EffectDef.RenderProcess.AllAtOnce:
                scriptRuntime!.ExecuteRender();
                EffectIndex++;
                Y = (int)(camera.Position.Y - LayerMargin)/20;
                X = (int)(camera.Position.X - LayerMargin)/20;
                if (EffectIndex >= level.Effects.Count)
                {
                    IsDone = true;
                    return;
                }

                scriptRuntime = new(level.Effects[EffectIndex], level, camera, layers, LayerMargin);
                break;

            case EffectDef.RenderProcess.PerRow:
                {
                    if (Y >= Rows || Y >= level.Height)
                    {
                        EffectIndex++;
                        Y = (int)(camera.Position.Y - LayerMargin)/20;
                        X = (int)(camera.Position.X - LayerMargin)/20;
                        scriptRuntime?.Dispose();

                        if (EffectIndex >= level.Effects.Count)
                        {
                            IsDone = true;
                            return;
                        }

                        scriptRuntime = new(level.Effects[EffectIndex], level, camera, layers, LayerMargin);
                        return;
                    }

                    scriptRuntime!.ExecuteRender(row: Y++);
                }
                break;

            case EffectDef.RenderProcess.PerCell:
                {
                    if (Y >= Rows || Y >= level.Height)
                    {
                        EffectIndex++;
                        Y = (int)(camera.Position.Y - LayerMargin)/20;
                        scriptRuntime?.Dispose();

                        if (EffectIndex >= level.Effects.Count)
                        {
                            IsDone = true;
                            return;
                        }

                        scriptRuntime = new(level.Effects[EffectIndex], level, camera, layers, LayerMargin);
                        return;
                    }

                    if (X >= Columns || X >= level.Width)
                    {
                        Y++;
                        X = (int)(camera.Position.X - LayerMargin)/20;
                        return;
                    }

                    if (level.Effects[EffectIndex].Matrix.IsInBounds(X, Y, 0)) 
                        scriptRuntime!.ExecuteRender(X, Y);

                    X++;
                }
                break;
        }
    }

    ~EffectRenderer()
    {
        scriptRuntime?.Dispose();
    }
}