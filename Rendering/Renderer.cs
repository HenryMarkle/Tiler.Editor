using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using Serilog;

namespace Tiler.Editor.Rendering;

public class Renderer
{
    public const int Width = 1400;
    public const int Height = 800;

    public Level Level { get; init; }
    public TileDex Tiles { get; init; }
    public PropDex Props { get; init; }
    public int SublayersPerLayer { get; init; } = 10;
    public int LayerMargin { get; init; } = 100;

    public LevelCamera SelectedCamera { get; private set; }

    public Managed.RenderTexture[] Layers { get; private set; }
    public Managed.RenderTexture Lightmap { get; private set; }
    public Managed.RenderTexture ComposedLightmap { get; private set; }
    public Managed.RenderTexture FinalLightmap { get; private set; }
    public Managed.RenderTexture Final { get; private set; }

    private TileRenderer TileRenderer { get; init; }
    private PropRenderer PropRenderer { get; init; }
    private EffectRenderer EffectRenderer { get; set; }

    public Renderer(Level level, TileDex tiles, PropDex props, LevelCamera? camera = null)
    {
        Level = level;
        Tiles = tiles;
        Props = props;

        Layers = new Managed.RenderTexture[level.Depth * SublayersPerLayer];
        for (var l = 0; l < Layers.Length; l++) Layers[l] =
            new(Width + LayerMargin * 2, Height + LayerMargin * 2, new Color4(0, 0, 0, 0), true);

        Lightmap = new(level.Lightmap.Width, level.Lightmap.Height, new Color4(0, 0, 0, 0), true);

        ComposedLightmap = new(Width, Height);
        FinalLightmap = new(Width, Height);
        Final = new(Width, Height);

        SelectedCamera = camera ?? level.Cameras.FirstOrDefault()
            ?? throw new RenderException("Level must have at least one camera");

        TileRenderer = new TileRenderer(Layers, Level, SelectedCamera);
        PropRenderer = new PropRenderer(Layers, Level, Props, SelectedCamera);
        EffectRenderer = new EffectRenderer(Layers, Level, SelectedCamera);
    }

    public enum RenderState
    {
        Idle,
        Tiles,
        Props,
        Poles,
        Effects,
        Lighting,
        Done,
        Aborted
    }

    public RenderState State { get; private set; }

    public void Next()
    {
        if (State is RenderState.Done or RenderState.Aborted) return;

        switch (State)
        {
            case RenderState.Done: return;
            case RenderState.Idle: State = RenderState.Tiles; return;
            case RenderState.Tiles:
                {
                    TileRenderer.Next();
                    if (TileRenderer.IsDone) State = RenderState.Props;
                }
                break;
            case RenderState.Props:
                {
                    PropRenderer.Next();
                    if (PropRenderer.IsDone) State = RenderState.Poles;
                }
                break;
            case RenderState.Poles:
                {
                    var columns = (Width + LayerMargin * 2) / 20;
                    var rows = (Height + LayerMargin * 2) / 20;

                    for (var z = 0; z < 5; z++)
                    {
                        Raylib.BeginTextureMode(Layers[z * SublayersPerLayer + 4]);
                        for (var y = 0; y < rows; y++)
                        {
                            var my = y + (int)(SelectedCamera.Position.Y / 20) - (LayerMargin / 20);
                            if (my < 0 || my >= Level.Height) continue;

                            for (var x = 0; x < columns; x++)
                            {
                                var mx = x + (int)(SelectedCamera.Position.X / 20) - (LayerMargin / 20);
                                if (mx < 0 || mx >= Level.Width) continue;

                                switch (Level.Geos[mx, my, z])
                                {
                                    case Geo.VerticalPole:
                                        Raylib.DrawRectangleRec(
                                            rec: new Rectangle(
                                                    mx * 20 + 8 + LayerMargin - SelectedCamera.Position.X,
                                                    Layers[z * SublayersPerLayer + 4].Height - 20 - (my * 20 + LayerMargin - SelectedCamera.Position.Y),
                                                    4,
                                                    20
                                                ),
                                            color: Color.Red
                                        );
                                        break;

                                    case Geo.HorizontalPole:
                                        Raylib.DrawRectangleRec(
                                            rec: new Rectangle(
                                                mx * 20 + LayerMargin - SelectedCamera.Position.X,
                                                Layers[z * SublayersPerLayer + 4].Height - 20 + 8 - (my * 20 + LayerMargin - SelectedCamera.Position.Y),
                                                20,
                                                4
                                            ),
                                            color: Color.Red
                                        );
                                        break;

                                    case Geo.CrossPole:
                                        Raylib.DrawRectangleRec(
                                            rec: new Rectangle(
                                                    mx * 20 + 8 + LayerMargin - SelectedCamera.Position.X,
                                                    Layers[z * SublayersPerLayer + 4].Height - 20 - (my * 20 + LayerMargin - SelectedCamera.Position.Y),
                                                    4,
                                                    20
                                                ),
                                            color: Color.Red
                                        );
                                        Raylib.DrawRectangleRec(
                                            rec: new Rectangle(
                                                mx * 20 + LayerMargin - SelectedCamera.Position.X,
                                                Layers[z * SublayersPerLayer + 4].Height - 20 + 8 - (my * 20 + LayerMargin - SelectedCamera.Position.Y),
                                                20,
                                                4
                                            ),
                                            color: Color.Red
                                        );
                                        break;
                                }
                            }
                        }
                        Raylib.EndTextureMode();
                    }


                    State = RenderState.Effects;
                }
                break;
            case RenderState.Effects:
                {
                    if (EffectRenderer.IsDone) State = RenderState.Done;

                    var count = 0;
                    while (!EffectRenderer.IsDone && ++count < 100) 
                        EffectRenderer.Next();
                }
                break;
            case RenderState.Lighting:
                {

                }
                break;
        }
    }

    public void Abort()
    {
        State = RenderState.Aborted;
    }
}