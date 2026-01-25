using System.Collections.Generic;
using System.Linq;
using System.IO;
using Raylib_cs;
using Serilog;
using System.Text;
using System;

namespace Tiler.Editor.Rendering;

public class Renderer
{
    public const int Width = 1400;
    public const int Height = 800;

    public struct Configuration
    {
        public bool AllCameras;
        public bool[] Cameras;
        public bool Geometry;

        public bool Tiles;
        public bool Props;
        public bool Effects;
        public bool Light;

        public Configuration()
        {
            AllCameras = true;
            Cameras = [];
            Geometry = true;
            
            Tiles = true;
            Props = true;
            Effects = true;
            Light = true;
        }
    }

    public enum ConnectionTypes
    {
        Dead,
        Shortcut,
        Exit,
        Spawn,
        Warp
    }
    public struct Connection()
    {
        public ConnectionTypes Type = ConnectionTypes.Dead;
    
        public List<(int x, int y, int z)> Path = [];
    }

    public Configuration Config { get; init; }

    public Level Level { get; init; }
    public string OutputDir { get; init; }
    public TileDex Tiles { get; init; }
    public PropDex Props { get; init; }
    public int SublayersPerLayer { get; init; } = 10;
    public int LayerMargin { get; init; } = 100;

    public int CurrentCameraIndex { get; private set; }

    public LevelCamera SelectedCamera => Level.Cameras[CurrentCameraIndex];

    public List<Connection> Connections = [];

    public Managed.RenderTexture[] Layers { get; private set; }
    public Managed.Texture Lightmap { get; private set; }
    public Managed.RenderTexture[] FinalRenders { get; private set; }

    public TileRenderer TileRenderer { get; private set; }
    public PropRenderer PropRenderer { get; private set; }
    public EffectRenderer EffectRenderer { get; private set; }
    public LightRenderer LightRenderer { get; private set; }

    public RenderEncoder? Encoder { get; private set; }

    public Renderer(
        Level level, 
        TileDex tiles, 
        PropDex props, 
        string outputDir
    ) {
        Config = new();
        
        Level = level;
        Tiles = tiles;
        Props = props;

        OutputDir = outputDir;

        Layers = new Managed.RenderTexture[level.Depth * SublayersPerLayer];
        for (var l = 0; l < Layers.Length; l++) Layers[l] =
            new(Width + LayerMargin * 2, Height + LayerMargin * 2, new Color4(0, 0, 0, 0), true);

        Lightmap = new Managed.Texture(Level.Lightmap);

        FinalRenders = new Managed.RenderTexture[Level.Cameras.Count];

        if (level.Cameras.Count == 0) 
            throw new RenderException("Level must have at least one camera");

        TileRenderer = new TileRenderer(Layers, Level, SelectedCamera);
        PropRenderer = new PropRenderer(Layers, Level, Props, SelectedCamera);
        EffectRenderer = new EffectRenderer(Layers, Level, SelectedCamera);
        LightRenderer = new LightRenderer(Layers, Lightmap, Level.LightDistance, Level.LightDirection);
    }

    public enum RenderState
    {
        Idle,
        Tiles,
        Props,
        Poles,
        Connections,
        Effects,
        Lighting,
        Encoding,
        Finalizing,
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


                    State = RenderState.Connections;
                }
                break;
            case RenderState.Connections:
                {
                    bool connects(int x, int y) => Level.Connections.IsInBounds(x, y, 0)
                            && Level.Connections[x, y, 0] is not ConnectionType.None;

                    // Camera dimensions

                    var columns = (Width + LayerMargin*2) / 20;
                    var rows = (Height + LayerMargin*2) / 20;

                    for (var y = 0; y < rows; y++)
                    {
                        // Convert local coords to matrix coords

                        var my = y + (int)(SelectedCamera.Position.Y/20) - (LayerMargin/20);
                        if (my < 0 || my >= Level.Height) continue;

                        for (var x = 0; x < columns; x++)
                        {
                            var mx = x + (int)(SelectedCamera.Position.X/20) - (LayerMargin/20);
                            if (mx < 0 || mx >= Level.Width) continue;

                            if (Level.Connections[mx, my, 0] != ConnectionType.Entrance) 
                                continue;

                            var connection = new Connection();

                            var cx = mx;
                            var cy = my;

                            var prevPos = (-1, -1);

                            do
                            {
                                // Look for next path node

                                var left   = (cx - 1, cy) != prevPos && connects(cx - 1, cy);
                                var top    = (cx, cy - 1) != prevPos && connects(cx, cy - 1);
                                var right  = (cx + 1, cy) != prevPos && connects(cx + 1, cy);
                                var bottom = (cx, cy + 1) != prevPos && connects(cx, cy + 1);

                                prevPos = (cx, cy);

                                switch ((left, top, right, bottom))
                                {
                                    case (true, false, false, false): cx--; break;
                                    case (false, true, false, false): cy--; break;
                                    case (false, false, true, false): cx++; break;
                                    case (false, false, false, true): cy++; break;
                                    
                                    // Cross paths
                                    case (true, true, true, false): cy--; break;
                                    case (false, true, true, true): cx++; break;
                                    case (true, false, true, true): cy++; break;
                                    case (true, true, false, true): cx--; break;

                                    default: goto skipLooking;
                                }

                                if (Level.Connections[cx, cy, 0] is not ConnectionType.Path)
                                {
                                    connection.Type = Level.Connections[cx, cy, 0] switch
                                    {
                                        ConnectionType.Exit => ConnectionTypes.Exit,
                                        ConnectionType.Spawn => ConnectionTypes.Spawn,
                                        ConnectionType.Warp => ConnectionTypes.Warp,
                                        ConnectionType.Entrance => ConnectionTypes.Shortcut,
                                        _ => ConnectionTypes.Dead
                                    };
                                }

                                connection.Path.Add((cx, cy, 0));
                            }   
                            while (true);

                        skipLooking:

                            Connections.Add(connection);
                        }
                    }

                    // Draw

                    foreach (var connection in Connections)
                    {
                        // TODO: Complete this
                    }

                    State = RenderState.Effects;
                }
            break;
            case RenderState.Effects:
                {
                    if (EffectRenderer.IsDone) State = RenderState.Lighting;

                    var count = 0;
                    while (!EffectRenderer.IsDone && ++count < 100) 
                        EffectRenderer.Next();
                }
                break;
            
            case RenderState.Lighting:
                {
                    if (LightRenderer.IsDone) State = RenderState.Encoding;

                    LightRenderer.Next();
                }
                break;

            case RenderState.Encoding:
                {
                    // Encode

                    Encoder = new RenderEncoder(Layers, LightRenderer.Final, SelectedCamera);

                    Encoder.Encode();

                    FinalRenders[CurrentCameraIndex] = Encoder.Final;

                    // Reset buffers & renderers

                    if (CurrentCameraIndex + 1 >= Level.Cameras.Count)
                    {
                        State = RenderState.Finalizing;
                        return;
                    }

                    CurrentCameraIndex++;
                    Connections.Clear();

                    foreach (var layer in Layers) layer.Clear();

                    TileRenderer = new TileRenderer(Layers, Level, SelectedCamera);
                    PropRenderer = new PropRenderer(Layers, Level, Props, SelectedCamera);
                    EffectRenderer = new EffectRenderer(Layers, Level, SelectedCamera);
                    LightRenderer = new LightRenderer(Layers, Lightmap, Level.LightDistance, Level.LightDirection);
                }
            break;

            case RenderState.Finalizing:

            State = RenderState.Done;
            break;
        }
    }

    public void Abort()
    {
        State = RenderState.Aborted;
    }

    public void Export(string directory)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("Directory does not exist");

        var levelDir = Path.Combine(directory, Level.Name!);

        if (!Directory.Exists(levelDir))
            Directory.CreateDirectory(levelDir);

        // Export camera renders

        for (var l = 0; l < FinalRenders.Length; l++)
        {
            using var image = new Managed.Image(Raylib.LoadImageFromTexture(FinalRenders[l].Texture));
            Raylib.ExportImage(image, Path.Combine(levelDir, $"{l}.png"));
        }

        // Export rest

        var sb = new StringBuilder();

        sb.AppendLine($"id = {Level.Name}");
        sb.AppendLine($"width = {Level.Width}");
        sb.AppendLine($"height = {Level.Height}");
        sb.Append($"cameras = {string.Join('|', Level.Cameras.Select(c => $"{c.Position.X}/{c.Position.Y}"))}");
        
        sb.Append("\n\n---\n\n");
        
        // Serialize connections
        foreach (var connection in Connections)
        {
            sb.AppendLine(
                $"{connection.Path[0].x}/{connection.Path[0].y}"
                + $"|{connection.Type}"
                + '|'
                + string.Join('|', connection.Path.Skip(1).Select(p => $"{p.x}/{p.y}/{p.z}"))
            );
        }
        
        sb.Append("\n\n---\n\n");

        // Serialize geometry
        for (var z = 0; z < Level.Depth; z++)
        {
            for (var y = 0; y < Level.Height; y++)
            {
                for (var x = 0; x < Level.Width; x++)
                {
                    sb.Append(Level.Geos[x, y, z] is Geo.Air ? "" : Level.Geos[x, y, z]);

                    if (x < Level.Width - 1) sb.Append('|');
                }

                if (y < Level.Height - 1) sb.Append('|');
            }

            if (z < Level.Depth - 1) sb.Append('|');
        }

        // TODO: Complete this
    }
}