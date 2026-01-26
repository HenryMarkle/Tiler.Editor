using System.Collections.Generic;
using System.Linq;
using System.IO;
using Raylib_cs;
using Serilog;
using System.Text;
using System;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace Tiler.Editor.Rendering;

public class Renderer
{
    public const int Width = 1400;
    public const int Height = 800;

    private class ShortcutEntranceAtlas(Managed.Texture texture)
    {
        public readonly Managed.Texture Texture = texture;

        public enum Directions { Left, Top, Right, Bottom }

        public const int Width = 5 * 20;
        public const int Height = 5 * 20;

        public static Rectangle GetSource(Directions direction) => new((int)direction * Width, 0, Width, Height);
    }

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
    
        public List<(int x, int y)> Path = [];
    }

    public Configuration Config { get; init; }
    public AppDirectories Paths { get; set; }

    public Level Level { get; init; }
    public TileDex Tiles { get; init; }
    public PropDex Props { get; init; }
    public int SublayersPerLayer { get; init; } = 10;
    public int LayerMargin { get; init; } = 100;

    public int CurrentCameraIndex { get; private set; }

    public LevelCamera SelectedCamera => Level.Cameras[CurrentCameraIndex];

    public List<Connection> Connections = [];

    private readonly ShortcutEntranceAtlas shortcutEntranceAtlas;
    private Managed.Texture shortcutHorizontal;
    private Managed.Texture shortcutVertical;
    private Managed.Texture shortcutEnd;

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
        AppDirectories paths
    ) {
        Config = new();
        
        Level = level;
        Tiles = tiles;
        Props = props;
        Paths = paths;

        Layers = new Managed.RenderTexture[level.Depth * SublayersPerLayer];
        for (var l = 0; l < Layers.Length; l++) Layers[l] =
            new(Width + LayerMargin * 2, Height + LayerMargin * 2, new Color4(0, 0, 0, 0), true);

        Lightmap = new Managed.Texture(Level.Lightmap);

        FinalRenders = new Managed.RenderTexture[Level.Cameras.Count];

        var renderingTexturesDir = Path.Combine(paths.Textures, "rendering");

        shortcutEntranceAtlas = new ShortcutEntranceAtlas(
            new Managed.Texture(Raylib.LoadTexture(Path.Combine(renderingTexturesDir, "shortcut_entrance.png")))
        );
        shortcutHorizontal = new Managed.Texture(
            Raylib.LoadTexture(Path.Combine(renderingTexturesDir, "shortcut_horizontal.png"))
        );
        shortcutVertical = new Managed.Texture(
            Raylib.LoadTexture(Path.Combine(renderingTexturesDir, "shortcut_vertical.png"))
        );
        shortcutEnd = new Managed.Texture(
            Raylib.LoadTexture(Path.Combine(renderingTexturesDir, "shortcut_end.png"))
        );

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

                            var connection = new Connection() { Path = [(mx, my)] };

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

                                var current = Level.Connections[cx, cy, 0];

                                connection.Type = current switch
                                {
                                    ConnectionType.Exit => ConnectionTypes.Exit,
                                    ConnectionType.Spawn => ConnectionTypes.Spawn,
                                    ConnectionType.Warp => ConnectionTypes.Warp,
                                    ConnectionType.Entrance => ConnectionTypes.Shortcut,
                                    _ => ConnectionTypes.Dead
                                };

                                connection.Path.Add((cx, cy));

                                if (current is not ConnectionType.Path) break;
                            }   
                            while (true);

                        skipLooking:

                            Connections.Add(connection);
                        }
                    }

                    // Draw

                    bool isGeo(int x, int y, Geo geo) => Level.Geos.IsInBounds(x, y, 0)
                            && Level.Geos[x, y, 0] == geo;

                    ReadOnlySpan<int> shortcutEntranceRepeat = [1, 8, 1];

                    foreach (var connection in Connections)
                    {
                        // Determine entrance direction

                        {
                            var (x, y) = connection.Path[0];

                            var leftCon   = connects(x - 1, y);
                            var topCon    = connects(x, y - 1);
                            var rightCon  = connects(x + 1, y);
                            var bottomCon = connects(x, y + 1);

                            var leftGeo   = isGeo(x - 1, y, Geo.Air);
                            var topGeo    = isGeo(x, y - 1, Geo.Air);
                            var rightGeo  = isGeo(x + 1, y, Geo.Air);
                            var bottomGeo = isGeo(x, y + 1, Geo.Air);

                            var direction = ShortcutEntranceAtlas.Directions.Left;

                            // A dumb way for checking for direction; may need more validation
                            switch ((leftCon || rightGeo, topCon || bottomGeo, rightCon || leftGeo, bottomCon || topGeo)) {
                                case (false, false, true, false):
                                direction = ShortcutEntranceAtlas.Directions.Left;
                                break;

                                case (false, false, false, true):
                                direction = ShortcutEntranceAtlas.Directions.Top;
                                break;

                                case (true, false, false, false):
                                direction = ShortcutEntranceAtlas.Directions.Right;
                                break;

                                case (false, true, false, false):
                                direction = ShortcutEntranceAtlas.Directions.Bottom;
                                break;

                                default: goto skipEntrance;
                            }

                            var sourceRect = ShortcutEntranceAtlas.GetSource(direction);
                            var destRect = new Rectangle(
                                (x - 2) * 20 - SelectedCamera.Position.X + LayerMargin, 
                                (y - 2) * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                5 * 20, 
                                5 * 20
                            );

                            var depth = 0;

                            for (int l = 0; l < 3; l++)
                            {
                                for (var r = 0; r < shortcutEntranceRepeat[l]; r++)
                                {
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[depth],
                                        texture:     shortcutEntranceAtlas.Texture,
                                        source:      sourceRect with { Y = l * sourceRect.Height },
                                        destination: destRect,
                                        tint:        Color.White
                                    );

                                    depth++;
                                }
                            }
                        }

                    skipEntrance:

                        foreach (var (x, y) in connection.Path[1..^1])
                        {
                            var leftCon   = connects(x - 1, y);
                            var topCon    = connects(x, y - 1);
                            var rightCon  = connects(x + 1, y);
                            var bottomCon = connects(x, y + 1);

                            switch ((leftCon, topCon, rightCon, bottomCon))
                            {
                                case (true, false, true, false):
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[0],
                                        texture:     shortcutHorizontal,
                                        source:      new Rectangle(0, 0, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[1],
                                        texture:     shortcutHorizontal,
                                        source:      new Rectangle(0, 20, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    break;

                                case (false, true, false, true):
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[0],
                                        texture:     shortcutVertical,
                                        source:      new Rectangle(0, 0, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[1],
                                        texture:     shortcutVertical,
                                        source:      new Rectangle(0, 20, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    break;

                                case (true, true, false, false):
                                case (false, true, true, false):
                                case (false, false, true, true):
                                case (true, false, false, true):
                                case (true, true, true, true):
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[0],
                                        texture:     shortcutHorizontal,
                                        source:      new Rectangle(0, 0, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[1],
                                        texture:     shortcutHorizontal,
                                        source:      new Rectangle(0, 20, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );

                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[0],
                                        texture:     shortcutVertical,
                                        source:      new Rectangle(0, 0, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    RlUtils.DrawTextureRT(
                                        rt:          Layers[1],
                                        texture:     shortcutVertical,
                                        source:      new Rectangle(0, 20, 20, 20),
                                        destination: new Rectangle(
                                                        x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                        y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                        20, 
                                                        20
                                                     ),
                                        tint:        Color.White
                                    );
                                    break;
                            }
                        }

                        if (
                            connection.Path is { Count: > 1 } && 
                            Level.Connections[connection.Path[^1].x, connection.Path[^1].y, 0] is not ConnectionType.Entrance
                        ) {
                            var (x, y) = connection.Path[^1];

                            RlUtils.DrawTextureRT(
                                rt:          Layers[0],
                                texture:     shortcutEnd,
                                source:      new Rectangle(0, 0, 20, 20),
                                destination: new Rectangle(
                                                x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                20, 
                                                20
                                                ),
                                tint:        Color.White
                            );
                            RlUtils.DrawTextureRT(
                                rt:          Layers[1],
                                texture:     shortcutEnd,
                                source:      new Rectangle(0, 20, 20, 20),
                                destination: new Rectangle(
                                                x * 20 - SelectedCamera.Position.X + LayerMargin, 
                                                y * 20 - SelectedCamera.Position.Y + LayerMargin, 
                                                20, 
                                                20
                                                ),
                                tint:        Color.White
                            );
                        }


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

    public void Export()
    {
        if (!Directory.Exists(Paths.Levels))
            throw new DirectoryNotFoundException("Directory does not exist");

        var levelDir = Path.Combine(Paths.Levels, Level.Name!);

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
                + string.Join('|', connection.Path.Skip(1).Select(p => $"{p.x}/{p.y}"))
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