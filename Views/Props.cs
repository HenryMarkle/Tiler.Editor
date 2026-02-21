namespace Tiler.Editor.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Runtime.CompilerServices;

using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Tiler.Editor.Managed;
using Tiler.Editor.Views.Components;

public class Props : BaseView
{
    private readonly Cursor cursor;
    private int selectedPropMenuCategoryIndex;
    private string? selectedPropMenuCategory;
    private List<PropDef>? selectedPropMenuCategoryProps;
    private int selectedPropMenuIndex;
    private PropDef? selectedProp;
    private PropDef? hoveredProp;

    private readonly RenderTexture propPreview;
    private readonly RenderTexture propTooltip;

    private bool redrawMain;
    
    public enum Precision
    {
        Free,
        Half,
        One
    }

    private Precision transformPrecision;
    private Precision gridPrecision;

    private Vector2 TransPos => transformPrecision switch
    {
        Precision.Half => new Vector2((int)(cursor.X / 10) * 10, (int)(cursor.Y / 10) * 10),
        Precision.One => new Vector2((int)(cursor.X / 20) * 20, (int)(cursor.Y / 20) * 20),
        _ => cursor.Pos,
    };

    private enum EditMode
    {
        Selection,
        Placement,
        Rope
    }

    private EditMode editMode;

    private enum SelectionAction
    {
        Nothing,
        Translate,
        Rotate,
        Scale,
        Deform
    }

    private SelectionAction selectionAction;
    
    private RopeModel? ropeModel;
    private RopeProperties? ropeModelProperties;

    private bool isSelecting;
    private Vector2 initialSelectionPos;
    private Rectangle selectionRect;

    private List<Prop> selectedPlacedProps;
    private readonly Timer unloadTimer;
    
    private Vector2 selectedPlacedPropsCenter;

    /// <summary>
    /// Used for translating props
    /// </summary>
    private Vector2 prevCursorPos;

    private float prevScaleCenterLen;

    private float prevRotateAngle;
    private float prevRotateCenterLen;

    /// <summary>
    /// 0 - none
    /// 1 - tl
    /// 2 - tr
    /// 3 - br
    /// 4 - bl
    /// </summary>
    private int deformVertex;

    //

    private readonly Raylib_cs.Shader invbShader;

    //
    private bool contPlacementLock;
    //

    private bool showSelectedPlacedPropsCenter;
    private bool individualOriginRotation;
    private bool continuousPlacement;

    public Props(Context context) : base(context)
    {
        cursor = new Cursor(context);

        transformPrecision = gridPrecision = Precision.Free;
        editMode = EditMode.Placement;
        selectionAction = SelectionAction.Nothing;

        selectedPlacedProps = [];

        unloadTimer = new Timer(
            callback: _ =>
            {
                if (Context.SelectedLevel is not { } level) return;
                foreach (var prop in level.Props) prop.Preview?.ToImage();
            },
            state:   null,
            dueTime: 0,
            period:  TimeSpan.FromSeconds(3).Milliseconds
        );

        propPreview = new RenderTexture(
            width:      1,
            height:     1,
            clearColor: new Color4(0, 0, 0, 0),
            clear:      true
        );
        propTooltip = new RenderTexture(
            width:      1,
            height:     1,
            clearColor: new Color4(0, 0, 0, 0),
            clear:      true
        );

        SelectPropCategory(index: 0);
        if (selectedProp is not null) DrawPropRT(propPreview, selectedProp);

        invbShader = LoadShader(
            vsFileName: Path.Combine(Context.Dirs.Shaders, "inverse_bilinear_interpolation.vs"),
            fsFileName: Path.Combine(Context.Dirs.Shaders, "inverse_bilinear_interpolation.fs")
        );
    }

    ~Props()
    {
        UnloadShader(invbShader);
        unloadTimer.Dispose();
    }

    /// <exception cref="NullReferenceException">If <see cref="selectedProp"/>
    /// or <see cref="Context.SelectedLevel"/> is null</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Prop CreateNewProp() => new Prop(
        def:    selectedProp!,
        config: selectedProp!.CreateConfig(),
        quad:   new Quad(
            new Rectangle(position: Vector2.Zero, size: new Vector2(propPreview.Width, propPreview.Height))
        ) + TransPos - (new Vector2(propPreview.Width, propPreview.Height)/2),
        depth:  Context.Layer * 10
    )
    {
        Preview = Context.SelectedLevel!.Props.Find(p => p.Def == selectedProp) is { } replica
            ? replica.Preview
            : new HybridImage(LoadImageFromTexture(propPreview.Texture))
    };
    
    private void SelectPropCategory(int index)
    {
        if (index >= Context.Props.Categories.Count) return;
        selectedPropMenuCategoryIndex = index;
        selectedPropMenuCategory = Context.Props.Categories[index];
        selectedPropMenuCategoryProps = Context.Props.CategoryProps[selectedPropMenuCategory];
        SelectPropFromCategory(index: 0);
    }
    private void SelectPropCategory(string category)
    {
        if (!Context.Props.CategoryProps.TryGetValue(category, out selectedPropMenuCategoryProps)) return;
        selectedPropMenuCategoryIndex = Context.Props.Categories.IndexOf(category);
        selectedPropMenuCategory = category;
        SelectPropFromCategory(index: 0);
    }

    private void SelectPropFromCategory(int index)
    {
        if (selectedPropMenuCategoryProps is null or { Count: 0 }) return;
        if (index >= selectedPropMenuCategoryProps.Count) return;

        selectedPropMenuIndex = index;
        selectedProp = selectedPropMenuCategoryProps[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnSelectAllPlacedProps()
    {
        if (Context.SelectedLevel is not { } level) return;

        foreach (var prop in level.Props) prop.IsSelected = false;
        selectedPlacedProps = [];

        CalculatePlacedPropsCenter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SelectPlacedProps(Predicate<Prop> predicate)
    {
        if (Context.SelectedLevel is not { } level) return;

        selectedPlacedProps.Clear();
        foreach (var prop in level.Props)
        {
            if (!predicate(prop))
            {
                prop.IsSelected = false;
                continue;
            }

            prop.IsSelected = true;
            selectedPlacedProps.Add(prop);
        }

        CalculatePlacedPropsCenter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalculatePlacedPropsCenter() => selectedPlacedPropsCenter = GetPropsCenter(selectedPlacedProps);

    private static Vector2 GetPropsCenter(List<Prop> props)
    {
        if (props.Count == 0) return Vector2.Zero;

        var center = props[0].Quad.Center;

        return props.Count == 1 
            ? center 
            : props.Skip(1).Aggregate(center, (current, prop) => (current + prop.Quad.Center) / 2);
    }

    public void DrawPropRT(RenderTexture rt, PropDef prop)
    {
        rt.Clear();

        switch (prop)
        {
            case VoxelStruct voxels:
                {
                    if (rt.Width != voxels.Width || rt.Height != voxels.Height)
                        rt.CleanResize(voxels.Width, voxels.Height);

                    voxels.Image.ToTexture();

                    for (var l = voxels.Layers - 1; l > -1; l--)
                    {
                        RlUtils.DrawTextureRT(
                            rt,
                            texture:     voxels.Image,
                            source:      new Rectangle(0, l * voxels.Height, voxels.Width, voxels.Height),
                            destination: new Rectangle(0, 0, voxels.Width, voxels.Height),
                            tint:        Color.White with { A = (byte)(255 - l) }
                        );
                    }
                }
                break;

            case Soft soft:
                {
                    if (rt.Width != soft.Width || rt.Height != soft.Height)
                        rt.CleanResize(soft.Width, soft.Height);

                    soft.Image.ToTexture();

                    RlUtils.DrawTextureRT(
                        rt,
                        texture:     soft.Image,
                        source:      new Rectangle(0, 0, soft.Width, soft.Height),
                        destination: new Rectangle(0, 0, soft.Width, soft.Height),
                        tint:        Color.White
                    );
                }
                break;

            case Antimatter antimatter:
                {
                    if (rt.Width != antimatter.Width || rt.Height != antimatter.Height)
                        rt.CleanResize(antimatter.Width, antimatter.Height);

                    antimatter.Image.ToTexture();

                    RlUtils.DrawTextureRT(
                        rt,
                        texture:     antimatter.Image,
                        source:      new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                        destination: new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                        tint:        Color.White
                    );
                }
                break;

            case Custom custom:
                {
                    if (rt.Width != custom.Width || rt.Height != custom.Height)
                        rt.CleanResize(custom.Width, custom.Height);

                    custom.Image.ToTexture();

                    RlUtils.DrawTextureRT(
                        rt,
                        texture:     custom.Image,
                        source:      new Rectangle(0, 0, custom.Width, custom.Height),
                        destination: new Rectangle(0, 0, custom.Width, custom.Height),
                        tint:        Color.White
                    );
                }
                break;

            default: return;
        }
    }

    public void DrawTilesViewport(int layer)
    {
        if (Context.SelectedLevel is not { } level) return;
        if (layer < 0 || layer >= Context.Viewports.Depth) return;

        BeginTextureMode(Context.Viewports.Tiles[layer]);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                var tile = level.Tiles[x, y, layer];
                if (tile is null) continue;

                var geo = level.Geos[x, y, layer];

                // BeginBlendMode(BlendMode.Custom);
                // Rlgl.SetBlendMode(BlendMode.Custom);
                // Rlgl.SetBlendFactors(1, 0, 1);

                switch (geo)
                {
                    case Geo.Solid:
                    case Geo.Wall:
                        DrawRectangle(posX: x * 20 + 4, posY: y * 20 + 4, width: 20 - 8, height: 20 - 8, tile.Color);
                        break;

                    case Geo.Slab:
                        DrawRectangle(posX: x * 20 + 4, posY: y * 20 + 10 + 4, width: 20 - 8, height: 10 - 8, tile.Color);
                        break;

                    case Geo.Platform:
                        DrawRectangle(posX: x * 20 + 4, posY: y * 20 + 4, width: 20 - 8, height: 10 - 8, tile.Color);
                        break;

                    case Geo.SlopeNW:
                        DrawTriangle(
                            v1: new Vector2((x + 1) * 20, y * 20),
                            v2: new Vector2(x * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeNE:
                        DrawTriangle(
                            v1: new Vector2(x * 20, y * 20),
                            v2: new Vector2(x * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeSE:
                        DrawTriangle(
                            v1: new Vector2((x + 1) * 20, y * 20),
                            v2: new Vector2(x * 20, y * 20),
                            v3: new Vector2(x * 20, (y + 1) * 20),
                            tile.Color
                        );
                        break;

                    case Geo.SlopeSW:
                        DrawTriangle(
                            v1: new Vector2(x * 20, y * 20),
                            v2: new Vector2((x + 1) * 20, (y + 1) * 20),
                            v3: new Vector2((x + 1) * 20, y * 20),
                            tile.Color
                        );
                        break;
                }
                // EndBlendMode();
            }
        }
        EndTextureMode();
    }

    public void DrawMainViewport()
    {
        if (Context.SelectedLevel is not { } level) return;

        BeginTextureMode(Context.Viewports.Main);
        ClearBackground(new Color(0, 0, 0, 0));
        for (int l = Context.Viewports.Depth - 1; l > -1; --l)
        {
            if (l == Context.Layer) continue;
            DrawTexture(Context.Viewports.Geos[l].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 120 });
            DrawTexture(Context.Viewports.Tiles[l].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 120 });
        }

        DrawRectangle(posX: 0, posY: 0, width: level.Width * 20, height: level.Height * 20, Color.Red with { A = 40 });

        DrawTexture(Context.Viewports.Geos[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.Black with { A = 210 });
        DrawTexture(Context.Viewports.Tiles[Context.Layer].Raw.Texture, posX: 0, posY: 0, Color.White with { A = 210 });
        EndTextureMode();
    }

    public void DrawPlacedProps()
    {
        if (Context.SelectedLevel is not { } level) return;

        foreach (var prop in level.Props)
        {
            if (prop.IsHidden) continue;

            if (prop.Preview is null)
            {
                if (level.Props.Find(p => p.Def == selectedProp) is { } replica)
                {
                    prop.Preview = replica.Preview;
                }
                else
                {
                    using var rt = new RenderTexture(width: 0, height: 0, new Color4(0,0,0,0));
                    DrawPropRT(rt, prop.Def);
                    prop.Preview = new HybridImage(LoadImageFromTexture(rt.Texture));
                }
            }
            else
            {
                prop.Preview.ToTexture();

                var layerTint = (byte)(255 - Math.Abs(prop.Depth - Context.Layer*10)/49.0f*220);

                var quad = new[]
                {
                    prop.Quad.TopLeft,
                    prop.Quad.TopRight,
                    prop.Quad.BottomRight,
                    prop.Quad.BottomLeft,
                };

                BeginShaderMode(invbShader);

                SetShaderValueV(
                    invbShader, 
                    locIndex: GetShaderLocation(invbShader, uniformName: "vertex_pos"), 
                    quad, 
                    ShaderUniformDataType.Vec2, 
                    count: 4
                );

                // DrawTexturePro(
                //     texture,
                //     source: new Rectangle(0, 0, texture.Width, texture.Height),
                //     dest: prop.Quad.Enclosed(),
                //     Vector2.Zero,
                //     0,
                //     new Color4(layerTint, layerTint, layerTint, layerTint)
                // );

                RlUtils.DrawTextureQuad(
                    texture: prop.Preview,
                    source: new Rectangle(0, 0, prop.Preview.Width, prop.Preview.Height),
                    quad:   prop.Quad,
                    tint:   new Color4(layerTint, layerTint, layerTint, layerTint)
                );

                EndShaderMode();
            }
        }
    }
    public void UpdatePlacedPropPreview(Prop prop)
    {
        throw new NotImplementedException();
    }

    public override void OnViewSelected()
    {
        redrawMain = true;
    }

    public override void Process()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (cursor.IsInWindow) return;
        
        cursor.ProcessCursor();

        if (IsKeyPressed(KeyboardKey.L))
        {
            Context.Layer = ++Context.Layer % Context.SelectedLevel?.Depth ?? 3;
            redrawMain = true;
        }

        if (IsKeyPressed(KeyboardKey.P))
        {
            transformPrecision = transformPrecision switch
            {
                Precision.Free => Precision.Half,
                Precision.Half => Precision.One,

                _ => Precision.Free
            };
        }

        if (IsKeyPressed(KeyboardKey.G))
        {
            gridPrecision = gridPrecision switch
            {
                Precision.Free => Precision.One,
                Precision.One => Precision.Half,
                Precision.Half => Precision.Free,

                _ => Precision.Free
            };
        }

        #region Process Control

        switch (editMode)
        {
            case EditMode.Placement:
            {
                // Avoid loop
                if (IsMouseButtonDown(MouseButton.Left) && !IsMouseButtonDown(MouseButton.Right) && !contPlacementLock)
                {
                    editMode = EditMode.Selection;
                    selectionAction = SelectionAction.Nothing;
                    goto selection_mode_case;
                }

                // Nearly identical branches
                // TODO: Try to optimize this

                if (continuousPlacement)
                {
                    if (IsMouseButtonDown(MouseButton.Right))
                    {
                        contPlacementLock = true;

                        if (selectedProp is null) break;

                        var previewSize = new Vector2(propPreview.Width, propPreview.Height);
                        var previewRect = new Rectangle(position: TransPos - (previewSize/2), previewSize);

                        // Must collide with no other props
                        // NOTE: Gets slower the more props are placed
                        // TODO: Optimize this
                        if (level.Props.All(p => !CheckCollisionRecs(p.Quad.Enclosed(), previewRect)))
                        {    
                            var quad = new Quad(
                                new Rectangle(position: Vector2.Zero, previewSize)
                            ) + TransPos - (previewSize/2);

                            var prop = new Prop(
                                def:    selectedProp,
                                config: selectedProp.CreateConfig(),
                                quad,
                                depth:  Context.Layer * 10
                            )
                            {
                                Preview = level.Props.Find(p => p.Def == selectedProp) is { } replica
                                    ? replica.Preview
                                    : new HybridImage(LoadImageFromTexture(propPreview.Texture))
                            };

                            level.Props.Add(prop);
                        }
                    }

                    if (IsMouseButtonReleased(MouseButton.Right) && contPlacementLock) 
                        contPlacementLock = false;
                }
                else
                {
                    if (IsMouseButtonPressed(MouseButton.Right))
                    {
                        if (selectedProp is null) break;

                        var previewSize = new Vector2(propPreview.Width, propPreview.Height);

                        var quad = new Quad(
                            new Rectangle(position: Vector2.Zero, previewSize)
                        ) + TransPos - (previewSize/2);

                        var prop = new Prop(
                            def:    selectedProp,
                            config: selectedProp.CreateConfig(),
                            quad,
                            depth:  Context.Layer * 10
                        )
                        {
                            Preview = level.Props.Find(p => p.Def == selectedProp) is { } replica
                                ? replica.Preview
                                : new HybridImage(LoadImageFromTexture(propPreview.Texture))
                        };

                        level.Props.Add(prop);
                    }
                }

                        
            }
                break;

            case EditMode.Selection:
                selection_mode_case:
            {
                // Avoid loop
                if (IsMouseButtonPressed(MouseButton.Right) && !IsMouseButtonDown(MouseButton.Left))
                {
                    editMode = EditMode.Placement;
                    selectionAction = SelectionAction.Nothing;

                    UnSelectAllPlacedProps();

                    break;
                }

                if (selectedPlacedProps.Count > 0)
                {
                            
                    if (IsKeyPressed(KeyboardKey.X))
                    {
                        level.Props = [..level.Props.Where(p => !p.IsSelected)];
                        UnSelectAllPlacedProps();
                        selectionAction = SelectionAction.Nothing;
                    }

                    #region Process Selection Control

                    if (IsKeyPressed(KeyboardKey.F))
                    {
                        selectionAction = selectionAction == SelectionAction.Translate 
                            ? SelectionAction.Nothing 
                            : SelectionAction.Translate;

                        if (selectionAction == SelectionAction.Translate)
                        {
                            prevCursorPos = cursor.Pos;
                        }
                    }
                    else if (IsKeyPressed(KeyboardKey.S))
                    {
                        selectionAction = selectionAction == SelectionAction.Scale 
                            ? SelectionAction.Nothing 
                            : SelectionAction.Scale;

                        if (selectionAction == SelectionAction.Scale)
                        {
                            prevScaleCenterLen = (cursor.Pos.X - selectedPlacedPropsCenter.X) + (cursor.Pos.Y - selectedPlacedPropsCenter.Y);
                        }
                    }
                    else if (IsKeyPressed(KeyboardKey.R))
                    {
                        selectionAction = selectionAction == SelectionAction.Rotate 
                            ? SelectionAction.Nothing 
                            : SelectionAction.Rotate;

                        prevRotateCenterLen = (cursor.Pos.X - selectedPlacedPropsCenter.X) + (cursor.Pos.Y - selectedPlacedPropsCenter.Y);
                    }
                    else if (IsKeyPressed(KeyboardKey.Q) && selectedPlacedProps.Count == 1)
                    {
                        selectionAction = selectionAction == SelectionAction.Deform 
                            ? SelectionAction.Nothing 
                            : SelectionAction.Deform;

                        deformVertex = 0;
                    }
                }

                switch (selectionAction)
                {
                    case SelectionAction.Nothing:
                        if (isSelecting)
                        {
                            if (IsMouseButtonDown(MouseButton.Left))
                            {
                                var minX = MathF.Min(initialSelectionPos.X, cursor.X);
                                var minY = MathF.Min(initialSelectionPos.Y, cursor.Y);
                                        
                                var maxX = MathF.Max(initialSelectionPos.X, cursor.X);
                                var maxY = MathF.Max(initialSelectionPos.Y, cursor.Y);

                                selectionRect.X = minX;
                                selectionRect.Y = minY;
                                selectionRect.Width = maxX - minX;
                                selectionRect.Height = maxY - minY;
                            }
                            else if (IsMouseButtonReleased(MouseButton.Left))
                            {
                                        
                                SelectPlacedProps(prop =>
                                {
                                    if (CheckCollisionRecs(
                                            selectionRect, 
                                            prop.Quad.Enclosed()
                                        ))
                                    {
                                        if (IsKeyDown(KeyboardKey.LeftControl)) 
                                            return !prop.IsSelected;
                                                
                                        return true;
                                    }

                                    return IsKeyDown(KeyboardKey.LeftControl) && prop.IsSelected;
                                });
                                        
                                if (selectedPlacedProps.Count > 0 && selectionRect is { Width: <= 0.1f, Height: <= 0.1f })
                                {
                                    var last = selectedPlacedProps[^1];
                                    foreach (var prop in level.Props) prop.IsSelected = prop == last;
                                    selectedPlacedProps = [last];
                                    CalculatePlacedPropsCenter();
                                }

                                isSelecting = false;
                                initialSelectionPos = -Vector2.One;
                                selectionRect = new Rectangle(-1, -1, width: -1, height: -1);
                            }
                        }
                        else if (IsMouseButtonDown(MouseButton.Left))
                        {
                            isSelecting = true;
                            initialSelectionPos = cursor.Pos;
                            selectionRect = new Rectangle(position: initialSelectionPos, size: Vector2.One);
                        }

                        if (selectedPlacedProps.Count > 0)
                        {    
                            // Duplicate selected props
                            if (IsKeyPressed(KeyboardKey.D))
                            {
                                List<Prop> copied = [..selectedPlacedProps.Select(p => new Prop(p))];
                                    
                                foreach (var prop in selectedPlacedProps) prop.IsSelected = false;
                                level.Props.AddRange(copied);
                                selectedPlacedProps = copied;
                                CalculatePlacedPropsCenter();

                                selectionAction = SelectionAction.Translate;
                                prevCursorPos = cursor.Pos;
                            }
                            else if (IsKeyPressed(KeyboardKey.M))
                            {
                                foreach (var prop in selectedPlacedProps) 
                                    prop.Depth = ++prop.Depth % 50;
                            }
                            else if (IsKeyPressed(KeyboardKey.N))
                            {
                                foreach (var prop in selectedPlacedProps)
                                    prop.Depth = Math.Clamp(prop.Depth - 1, 0, 49);
                            }
                            else if (IsKeyPressed(KeyboardKey.T))
                            {
                                foreach (var prop in selectedPlacedProps)
                                {
                                    if (prop.Preview is null) continue;

                                    var previewSize = new Vector2(prop.Preview.Width, prop.Preview.Height);

                                    var quad = new Quad(
                                        new Rectangle(position: Vector2.Zero, previewSize)
                                    ) + prop.Quad.Center - (previewSize/2);

                                    prop.Quad = quad;
                                }
                            }
                            else if (IsKeyPressed(KeyboardKey.Y))
                            {
                                var last = selectedPlacedProps[^1].Def;

                                if (last != selectedProp) DrawPropRT(propPreview, last);
                                        
                                if (last.Category is not null)
                                {
                                    SelectPropCategory(last.Category);
                                    if (selectedPropMenuCategoryProps is not null)
                                    {
                                        SelectPropFromCategory(selectedPropMenuCategoryProps.FindIndex(p => p == last));
                                    }
                                }
                            }
                        }

                        break;

                    case SelectionAction.Translate:
                    {
                        var delta = cursor.Pos - prevCursorPos;
                                    
                        foreach (var prop in selectedPlacedProps) prop.Quad += delta;

                        CalculatePlacedPropsCenter();

                        prevCursorPos = cursor.Pos;

                        if (IsMouseButtonPressed(MouseButton.Left))
                            selectionAction = SelectionAction.Nothing;
                    }
                        break;

                    case SelectionAction.Scale:
                    {
                        var centerLen = (cursor.Pos.X - selectedPlacedPropsCenter.X) + (cursor.Pos.Y - selectedPlacedPropsCenter.Y);
                        var delta = centerLen - prevScaleCenterLen;

                        if (delta != 0)
                            foreach (var prop in selectedPlacedProps)
                            {
                                // if (delta < 0)
                                // {
                                //     var enclosed = prop.Quad.Enclosed();

                                //     if (enclosed.Width + delta < 10 || enclosed.Height + delta < 10) continue;
                                // }

                                // TODO: Clamp scaling to avoid vertex mishandling 

                                prop.Quad.TopLeft = Raymath.Vector2Add(
                                    Raymath.Vector2Add(
                                        Raymath.Vector2Subtract(prop.Quad.TopLeft, selectedPlacedPropsCenter),
                                        Raymath.Vector2Normalize(Raymath.Vector2Subtract(prop.Quad.TopLeft, selectedPlacedPropsCenter)) 
                                        *delta
                                    ),
                                    selectedPlacedPropsCenter
                                );
                                prop.Quad.TopRight = Raymath.Vector2Add(
                                    Raymath.Vector2Add(
                                        Raymath.Vector2Subtract(prop.Quad.TopRight, selectedPlacedPropsCenter),
                                        Raymath.Vector2Normalize(Raymath.Vector2Subtract(prop.Quad.TopRight, selectedPlacedPropsCenter)) 
                                        *delta
                                    ),
                                    selectedPlacedPropsCenter
                                );
                                prop.Quad.BottomRight = Raymath.Vector2Add(
                                    Raymath.Vector2Add(
                                        Raymath.Vector2Subtract(prop.Quad.BottomRight, selectedPlacedPropsCenter),
                                        Raymath.Vector2Normalize(Raymath.Vector2Subtract(prop.Quad.BottomRight, selectedPlacedPropsCenter)) 
                                        *delta
                                    ),
                                    selectedPlacedPropsCenter
                                );
                                prop.Quad.BottomLeft = Raymath.Vector2Add(
                                    Raymath.Vector2Add(
                                        Raymath.Vector2Subtract(prop.Quad.BottomLeft, selectedPlacedPropsCenter),
                                        Raymath.Vector2Normalize(Raymath.Vector2Subtract(prop.Quad.BottomLeft, selectedPlacedPropsCenter)) 
                                        *delta
                                    ),
                                    selectedPlacedPropsCenter
                                );
                            }

                        prevScaleCenterLen = centerLen;

                        if (IsMouseButtonPressed(MouseButton.Left))
                            selectionAction = SelectionAction.Nothing;
                    }
                        break;

                    case SelectionAction.Rotate:
                    {
                        var centerLen = (cursor.Pos.X - selectedPlacedPropsCenter.X) + (cursor.Pos.Y - selectedPlacedPropsCenter.Y);
                        var delta = centerLen - prevRotateCenterLen;
                                    
                        var angle = float.RadiansToDegrees(MathF.Atan2(
                            y: cursor.Y - selectedPlacedPropsCenter.Y,
                            x: cursor.X - selectedPlacedPropsCenter.X
                        ));
                        var rotateDelta = angle - prevRotateAngle;

                        if (IsKeyDown(KeyboardKey.LeftControl))
                        {
                            foreach (var prop in selectedPlacedProps)
                                prop.Quad.Rotate(degrees: (int)MathF.Ceiling(rotateDelta), selectedPlacedPropsCenter);
                        }
                        else
                        {
                            foreach (var prop in selectedPlacedProps)
                                prop.Quad.Rotate(
                                    degrees: (int)MathF.Ceiling(delta), 
                                    individualOriginRotation 
                                        ? prop.Quad.Center 
                                        : selectedPlacedPropsCenter
                                );
                        }

                        prevRotateAngle = angle;
                        prevRotateCenterLen = centerLen;

                        if (IsMouseButtonPressed(MouseButton.Left))
                            selectionAction = SelectionAction.Nothing;
                    }
                        break;

                    case SelectionAction.Deform:
                    {
                        // Only one selected prop must be guaranteed.
                        var prop = selectedPlacedProps[0];

                        if (deformVertex == 0)
                        {
                            if (IsMouseButtonDown(MouseButton.Left))
                            {
                                if (CheckCollisionPointCircle(cursor.Pos, center: prop.Quad.TopLeft, radius: 10)) deformVertex = 1;
                                else if (CheckCollisionPointCircle(cursor.Pos, center: prop.Quad.TopRight, radius: 10)) deformVertex = 2;
                                else if (CheckCollisionPointCircle(cursor.Pos, center: prop.Quad.BottomRight, radius: 10)) deformVertex = 3;
                                else if (CheckCollisionPointCircle(cursor.Pos, center: prop.Quad.BottomLeft, radius: 10)) deformVertex = 4;
                            }
                        }
                        else
                        {
                            switch (deformVertex)
                            {
                                case 1: prop.Quad.TopLeft = cursor.Pos; break;
                                case 2: prop.Quad.TopRight = cursor.Pos; break;
                                case 3: prop.Quad.BottomRight = cursor.Pos; break;
                                case 4: prop.Quad.BottomLeft = cursor.Pos; break;
                            }

                            if (IsMouseButtonReleased(MouseButton.Left)) deformVertex = 0;
                        }
                    }
                        break;
                }

                #endregion
            }
                break;

            case EditMode.Rope:
            {
                // TODO: Complete this
                if (IsKeyPressed(KeyboardKey.A)) editMode = EditMode.Selection;
            }
                break;
        }

        #endregion
    }

    public override void Draw()
    {
        if (Context.SelectedLevel is not { } level) return;
        if (redrawMain)
        {
            DrawTilesViewport(layer: 0);
            DrawTilesViewport(layer: 1);
            DrawTilesViewport(layer: 2);
            DrawMainViewport();

            redrawMain = false;
        }

        BeginMode2D(Context.Camera);
        DrawTexture(
            texture: Context.Viewports.Main.Raw.Texture,
            posX:    0,
            posY:    0,
            tint:    Color.White
        );

        switch (gridPrecision)
        {
            case Precision.One:
                for (var x = 0; x < level.Width; x++)
                    DrawLineEx(new Vector2(x * 20, 0), new Vector2(x * 20, level.Height * 20), x % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
                for (var y = 0; y < level.Height; y++)
                    DrawLineEx(new Vector2(0, y * 20), new Vector2(level.Width * 20, y * 20), y % 2 == 0 ? 2 : 1, Color.White with { A = 80 });
                break;

            case Precision.Half:
                for (var x = 0; x < level.Width * 2; x++)
                    DrawLineEx(new Vector2(x * 10, 0), new Vector2(x * 10, level.Height * 20), x % 2 == 0 ? 1 : 0.5f, Color.White with { A = 80 });
                for (var y = 0; y < level.Height * 2; y++)
                    DrawLineEx(new Vector2(0, y * 10), new Vector2(level.Width * 20, y * 10), y % 2 == 0 ? 1 : 0.5f, Color.White with { A = 80 });
                break;
        }

        /// TODO: Optimize using redraw queue

        float screenW = GetScreenWidth();
        float screenH = GetScreenHeight();
        var screenSize = new Vector2(screenW, screenH);

        DrawPlacedProps();

        #region Draw Control

        switch (editMode)
        {
            case EditMode.Placement:
                {
                    if (selectedProp is not null)
                    {
                        var previewSize = new Vector2(propPreview.Width, propPreview.Height);

                        DrawTexturePro(
                            texture: propPreview.Texture,
                            source: new Rectangle(0, 0, previewSize),
                            dest: new Rectangle(position: TransPos, previewSize),
                            origin: previewSize / 2,
                            rotation: 0,
                            tint: Color.White
                        );
                    }
                }
                break;

            case EditMode.Selection:
                switch (selectionAction) {
                    case SelectionAction.Nothing:
                    if (isSelecting)
                    {
                        DrawRectangleLinesEx(
                            rec:       selectionRect,
                            lineThick: 1f,
                            color:     Color.SkyBlue
                        );
                    }
                    break;

                    case SelectionAction.Translate: break;
                    case SelectionAction.Scale: break;
                    case SelectionAction.Rotate: break;
                    case SelectionAction.Deform:
                        {
                            var prop = selectedPlacedProps[0];

                            DrawCircleV(prop.Quad.TopLeft, radius: 8, Color.White);
                            DrawCircleV(prop.Quad.TopLeft, radius: 6, Color.Green);

                            DrawCircleV(prop.Quad.TopRight, radius: 8, Color.White);
                            DrawCircleV(prop.Quad.TopRight, radius: 6, Color.Green);

                            DrawCircleV(prop.Quad.BottomRight, radius: 8, Color.White);
                            DrawCircleV(prop.Quad.BottomRight, radius: 6, Color.Green);

                            DrawCircleV(prop.Quad.BottomLeft, radius: 8, Color.White);
                            DrawCircleV(prop.Quad.BottomLeft, radius: 6, Color.Green);
                        }
                        break;
                }
                break;
        }

        #endregion

        EndMode2D();
    }

    public override void GUI()
    {
        if (Context.SelectedLevel is not { } level) return;

        cursor.ProcessGUI();

        if (ImGui.Begin(name: "Props"))
        {
            ImGui.Columns(count: 2);

            if (ImGui.BeginListBox(label: "##Categories", size: ImGui.GetContentRegionAvail()))
            {
                for (var c = 0; c < Context.Props.Categories.Count; c++)
                {
                    var category = Context.Props.Categories[c];
                    if (ImGui.Selectable(category, c == selectedPropMenuCategoryIndex))
                    {
                        selectedPropMenuCategoryIndex = c;
                        selectedPropMenuCategory = category;
                        selectedPropMenuCategoryProps = Context.Props.CategoryProps[category];
                        selectedPropMenuIndex = 0;

                        if (selectedPropMenuCategoryProps.Count > 0)
                        {
                            if (selectedPropMenuCategoryProps[0] != selectedProp) 
                                DrawPropRT(propPreview, selectedPropMenuCategoryProps[0]);
                            selectedProp = selectedPropMenuCategoryProps[0];
                        }
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.NextColumn();

            if (ImGui.BeginListBox(label: "##Props", size: ImGui.GetContentRegionAvail()))
            {
                if (selectedPropMenuCategoryProps is not null)
                {
                    for (var p = 0; p < selectedPropMenuCategoryProps.Count; p++)
                    {
                        var prop = selectedPropMenuCategoryProps[p];

                        if (ImGui.Selectable(label: $"{prop.Name ?? prop.ID}##{prop.ID}", selected: selectedPropMenuIndex == p))
                        {
                            if (prop != selectedProp)
                            {
                                DrawPropRT(propPreview, prop);
                            }

                            selectedPropMenuIndex = p;
                            selectedProp = prop;
                            editMode = EditMode.Placement;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            if (prop != hoveredProp)
                            {
                                DrawPropRT(propTooltip, prop);
                                hoveredProp = prop;
                            }

                            ImGui.BeginTooltip();
                            rlImGui_cs.rlImGui.Image(propTooltip.Texture);
                            ImGui.EndTooltip();
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin(name: "Placed##PlacedProps"))
        {
            if (ImGui.BeginListBox(label: "##List", size: ImGui.GetContentRegionAvail()))
            {
                for (var p = 0; p < level.Props.Count; p++)
                {
                    var prop = level.Props[p];

                    if (ImGui.Selectable(label: $"{p}. {prop.Def.ID}", prop.IsSelected))
                    {
                        // Select a range
                        if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
                        {
                            var firstSelectedIndex = level.Props.FindIndex(p => p.IsSelected);

                            if (firstSelectedIndex == -1) firstSelectedIndex = 0;
                            for (var i = 0; i < level.Props.Count; i++)
                                level.Props[i].IsSelected = i >= firstSelectedIndex && i <= p;

                            selectedPlacedProps = [..level.Props.Where(p => p.IsSelected)];
                        }
                        // Select/Deselect
                        else if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
                        {
                            // Intentional
                            if (prop.IsSelected = !prop.IsSelected) 
                                selectedPlacedProps.Add(prop);
                            else
                                selectedPlacedProps.Remove(prop);
                        }
                        // Select only one
                        else
                        {
                            foreach (var toDeselect in selectedPlacedProps)
                                toDeselect.IsSelected = false;
                            
                            prop.IsSelected = !prop.IsSelected;
                            selectedPlacedProps.Clear();
                            selectedPlacedProps.Add(prop);
                        }

                        editMode = EditMode.Selection;
                        CalculatePlacedPropsCenter();
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();

        if (ImGui.Begin(name: "Options##PlacedPropsOptions"))
        {
            ImGui.Checkbox(label: "Selected Center", ref showSelectedPlacedPropsCenter);
            ImGui.Checkbox(label: "Individual Rotation", ref individualOriginRotation);

            if (selectedPlacedProps.Count > 0)
            {
                if (selectedPlacedProps.All(p => p.Depth == selectedPlacedProps[0].Depth))
                {
                    var depth = selectedPlacedProps[0].Depth;

                    if (ImGui.SliderInt(label: "Depth", v: ref depth, v_min: 0, v_max: 49))
                    {
                        foreach (var prop in selectedPlacedProps) 
                            prop.Depth = depth;
                    }
                }
                
                if (selectedPlacedProps.All(p => p.IsHidden) || selectedPlacedProps.All(p => !p.IsHidden))
                {
                    var hidden = selectedPlacedProps[0].IsHidden;

                    if (ImGui.Checkbox(label: "Hidden", ref hidden))
                    {
                        foreach (var p in selectedPlacedProps) p.IsHidden = hidden;
                    }
                }
            }
        }

        ImGui.End();


        if (ImGui.Begin(name: "Settings##PlacementSettings"))
        {
            if (selectedProp is null) ImGui.BeginDisabled();
            if (ImGui.Button(label: "Rope-ify"))
            {
                editMode = EditMode.Rope;

                ropeModel = new RopeModel(
                    level:      Context.SelectedLevel,
                    prop:       CreateNewProp(),
                    properties: ropeModelProperties ??= new RopeProperties( // TODO: Auto-adjust the initial values
                        segmentLength: 1,
                        collisionDepth: 1,
                        segmentRadius: 1,
                        gravity: 1,
                        friction: 1,
                        airFriction: 0.1f,
                        stiff: false,
                        edgeDirection: 0,
                        rigid: 0.1f,
                        selfPush: 0.1f,
                        sourcePush: 0
                    ),
                    segments:   [ cursor.Pos - (Vector2.UnitX * 20), cursor.Pos, cursor.Pos + (Vector2.UnitX * 20) ]
                );
            }
            if (selectedProp is null) ImGui.EndDisabled();
            
            {
                var pres = (int)gridPrecision;
                ImGui.SetNextItemWidth(80);
                if (ImGui.Combo(label: "Grid", ref pres, "None\0Half\0One"))
                    gridPrecision = (Precision)pres;               
            }
            
            {
                var pres = (int)transformPrecision;
                ImGui.SetNextItemWidth(80);
                if (ImGui.Combo(label: "Transform Precision", ref pres, "Free\0Half\0One"))
                    transformPrecision = (Precision)pres;               
            }

            ImGui.Checkbox(label: "Continuous Placement", ref continuousPlacement);
        }

        ImGui.End();
    }

    public override void Debug()
    {
        cursor.PrintDebug();

        var printer = Context.DebugPrinter;

        printer.PrintlnLabel("Layer", Context.Layer, Color.Magenta);

        printer.PrintlnLabel("Transform Precision", transformPrecision, Color.Magenta);
        printer.PrintlnLabel("Grid Precision", gridPrecision, Color.Magenta);
        
        printer.PrintlnLabel("Edit Mode", editMode, Color.Magenta);
        printer.PrintlnLabel("Selection Action", selectionAction, Color.Magenta);

        printer.PrintlnLabel("Selected Category", selectedPropMenuCategory, Color.Gold);
        printer.PrintlnLabel("Selected Prop", selectedProp, Color.Gold);

        printer.PrintlnLabel("Selected Placed Count", selectedPlacedProps.Count, Color.SkyBlue);
    }
}