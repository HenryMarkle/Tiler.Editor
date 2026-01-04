namespace Tiler.Editor.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Raylib_cs;
using Serilog;
using Tiler.Editor.Managed;
using Tiler.Editor.Rendering;
using static Raylib_cs.Raylib;

public class Render : BaseView
{
    public Render(Context context) : base(context)
    {
        composeShader = LoadShaderFromMemory(
            File.ReadAllText(Path.Combine(context.Dirs.Shaders, "inverse_bilinear_interpolation.vs")), 
            @"#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec2 vertex_pos[4];
uniform sampler2D texture0;
uniform float tintAccum;

out vec4 finalColor;

float cross2d(vec2 a, vec2 b) {
    return a.x * b.y - a.y * b.x;
}

// https://people.csail.mit.edu/bkph/articles/Quadratics.pdf
vec2 invbilinear_robust(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
    // Pre-compute constants (same as k1-k5 in C# code)
    vec2 k1 = c - d + a - b;
    float k2 = -4.0 * cross2d(k1, d - a);
    float k3 = cross2d(a - d, b - a);
    vec2 k4 = b - a;
    vec2 k5 = d - a;
    
    // Compute quadratic coefficients
    float b_coef = k3 - cross2d(k1, p - a);
    float c_coef = cross2d(p - a, k4);
    
    // Handle near-zero discriminant
    const float EPSILON = 1e-8;
    float discriminant = b_coef * b_coef + k2 * c_coef;
    
    if (discriminant < 0.0) {
        return vec2(-1.0);
    }
    
    float rad = sqrt(discriminant);
    
    // Avoid division by zero with numerically stable form
    float denom1 = b_coef + rad;
    float denom2 = b_coef - rad;
    
    // Choose the more stable solution
    float v1, v2;
    if (abs(denom1) > abs(denom2)) {
        v1 = -2.0 * c_coef / denom1;
        v2 = -2.0 * c_coef / denom2;
    } else {
        v2 = -2.0 * c_coef / denom2;
        v1 = -2.0 * c_coef / denom1;
    }
    
    // Compute u for both solutions using the stable dot product method
    vec2 e1 = p - a - k5 * v1;
    vec2 denom_vec1 = k4 + k1 * v1;
    float dot_e1 = dot(e1, e1);
    float dot_denom1 = dot(denom_vec1, e1);
    float u1 = (abs(dot_denom1) > EPSILON) ? dot_e1 / dot_denom1 : -1.0;
    
    vec2 e2 = p - a - k5 * v2;
    vec2 denom_vec2 = k4 + k1 * v2;
    float dot_e2 = dot(e2, e2);
    float dot_denom2 = dot(denom_vec2, e2);
    float u2 = (abs(dot_denom2) > EPSILON) ? dot_e2 / dot_denom2 : -1.0;
    
    // Choose the solution that's closer to [0,1] range
    float dist1 = max(abs(u1 - 0.5), abs(v1 - 0.5));
    float dist2 = max(abs(u2 - 0.5), abs(v2 - 0.5));
    
    return (dist1 <= dist2) ? vec2(u1, v1) : vec2(u2, v2);
}

// Alternative: Fallback iterative method for extreme cases
vec2 invbilinear_iterative(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
    vec2 uv = vec2(0.5, 0.5);
    
    const int MAX_ITERATIONS = 10;
    const float TOLERANCE = 1e-6;
    
    for (int i = 0; i < MAX_ITERATIONS; i++) {
        vec2 e = b - a;
        vec2 f = d - a;
        vec2 g = a - b + c - d;
        
        vec2 current = a + uv.x * e + uv.y * f + uv.x * uv.y * g;
        vec2 error = current - p;
        
        if (dot(error, error) < TOLERANCE * TOLERANCE) break;
        
        vec2 du = e + uv.y * g;
        vec2 dv = f + uv.x * g;
        
        float det = du.x * dv.y - du.y * dv.x;
        if (abs(det) < 1e-10) break;
        
        vec2 delta = vec2(
            (-error.x * dv.y + error.y * dv.x) / det,
            (error.x * du.y - error.y * du.x) / det
        );
        
        uv -= delta;
        uv = clamp(uv, vec2(0.0), vec2(1.0));
    }
    
    return uv;
}

void main()
{
    vec2 va = vertex_pos[0]; // top left
    vec2 vb = vertex_pos[1]; // top right
    vec2 vc = vertex_pos[2]; // bottom right
    vec2 vd = vertex_pos[3]; // bottom left

    vec2 uv = invbilinear_robust(fragTexCoord, va, vb, vc, vd);
    
    // Fallback to iterative if robust method fails
    const float TOLERANCE = 0.52; // Slightly more than 0.5 to account for boundary pixels
    if (max(abs(uv.x - 0.5), abs(uv.y - 0.5)) > TOLERANCE) {
        uv = invbilinear_iterative(fragTexCoord, va, vb, vc, vd);
    }

    uv.y = 1.0 - uv.y;
    
    // Final validation
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
        discard;
    }
    
    vec4 c = texture(texture0, uv);
    finalColor = clamp(vec4(c.r + tintAccum, c.g + tintAccum, c.b + tintAccum, c.a), vec4(0,0,0,0), vec4(1,1,1,1));
}"
        );

        preview = new RenderTexture(
            Renderer.Width, 
            Renderer.Height, 
            Color.White, 
            clear: true
        );
    }

    ~Render()
    {
        UnloadShader(composeShader);
    }

    private Renderer? renderer;

    private readonly Raylib_cs.Shader composeShader;
    // private readonly Shader vflipShader;

    private RenderTexture preview;

    private bool showOptions;
    private Renderer.Configuration rendererConfig;

    public override void OnLevelSelected(Level level)
    {
        rendererConfig = new()
        {
            AllCameras = true,
            Cameras = [..level.Cameras.Select(_ => true)]  
        };
    }

    public override void OnViewSelected()
    {
        if (Context.SelectedLevel is not { } level) return;

        level.Lightmap = new Managed.Image(LoadImageFromTexture(Context.Viewports.Lightmap.Texture));

        rendererConfig = new()
        {
            AllCameras = true,
            Cameras = [..level.Cameras.Select(_ => true)]  
        };
    }

    public override void Process()
    {
        if (renderer 
            is null or 
            { 
                State: Renderer.RenderState.Done or 
                Renderer.RenderState.Aborted 
            }
        ) return;

        try
        {
            renderer.Next();
        } 
        catch (RenderException re)
        {
            Log.Error("Failed to render level {Name}\n{Exception}", renderer.Level.Name, re);
            renderer.Abort();
        }

        preview.Clear();

        BeginTextureMode(preview);
        for (int l = renderer.Layers.Length - 1; l >= 0; l--)
        {
            BeginShaderMode(composeShader);
            var layer = renderer.Layers[l];

            SetShaderValueTexture(
                shader:   composeShader, 
                locIndex: GetShaderLocation(composeShader, "texture0"), 
                texture:  layer.Texture
            );

            var progress = l / (float)renderer.Layers.Length;

            SetShaderValue(
                shader:   composeShader,
                locIndex: GetShaderLocation(composeShader, "tintAccum"),
                value:    progress,
                ShaderUniformDataType.Float
            );

            // Initial positions

            var quad = new Quad(
                topLeft:     Vector2.Zero - (Vector2.One * renderer.LayerMargin),
                topRight:    (Vector2.UnitX * LevelCamera.Width) + new Vector2(renderer.LayerMargin, -renderer.LayerMargin),
                bottomRight: new Vector2(LevelCamera.Width + renderer.LayerMargin, LevelCamera.Height + renderer.LayerMargin),
                bottomLeft:  (Vector2.UnitY * LevelCamera.Height) + new Vector2(-renderer.LayerMargin, renderer.LayerMargin)
            );

            // Quad interpolation

            var camera = renderer.SelectedCamera;

            quad.TopLeft = Raymath.Vector2Lerp(
                v1: quad.TopLeft, 
                v2: quad.TopLeft + camera.TopLeft.Position, 
                amount: progress
            );

            quad.TopRight = Raymath.Vector2Lerp(
                v1: quad.TopRight, 
                v2: quad.TopRight + camera.TopRight.Position, 
                amount: progress
            );
            
            quad.BottomRight = Raymath.Vector2Lerp(
                v1: quad.BottomRight, 
                v2: quad.BottomRight + camera.BottomRight.Position, 
                amount: progress
            );
            
            quad.BottomLeft = Raymath.Vector2Lerp(
                v1: quad.BottomLeft, 
                v2: quad.BottomLeft + camera.BottomLeft.Position, 
                amount: progress
            );

            // Vertical flipping

            quad = new Quad(
                topLeft:     new(quad.BottomLeft.X, preview.Height - quad.BottomLeft.Y),
                topRight:    new(quad.BottomRight.X, preview.Height - quad.BottomRight.Y),
                bottomRight: new(quad.TopRight.X, preview.Height - quad.TopRight.Y),
                bottomLeft:  new(quad.TopLeft.X, preview.Height - quad.TopLeft.Y)
            );

            var quadArr = new Vector2[4]
            {
                quad.TopLeft,
                quad.TopRight,
                quad.BottomRight,
                quad.BottomLeft,
            };

            SetShaderValueV(
                composeShader, 
                GetShaderLocation(composeShader, "vertex_pos"), 
                quadArr, 
                ShaderUniformDataType.Vec2, 
                4
            );

            RlUtils.DrawTextureQuad(
                texture:     layer.Texture, 
                source:      new Rectangle(0, 0, layer.Width, layer.Height),
                quad
            );

            EndShaderMode();
        }
        EndTextureMode();
    }

    public override void GUI()
    {
        if (Context.SelectedLevel is not { } level) return;

        if (ImGui.Begin("Level Render", 
                ImGuiWindowFlags.NoMove | 
                ImGuiWindowFlags.NoResize | 
                ImGuiWindowFlags.NoCollapse)
        ) {
            ImGui.SetWindowPos(new Vector2(30, 40));
            ImGui.SetWindowSize(new Vector2(GetScreenWidth() - 60, GetScreenHeight() - 80));

            ImGui.ProgressBar(0);
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 200);

            var isDisabled = renderer?.State is not null and not (Renderer.RenderState.Done or Renderer.RenderState.Idle or Renderer.RenderState.Aborted);
            if (isDisabled) ImGui.BeginDisabled();
            if (ImGui.Button("Start", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                if (renderer is null or { State: Renderer.RenderState.Done })
                {
                    renderer = new(Context.SelectedLevel!, Context.Tiles, Context.Props, Context.Dirs.Levels)
                    {
                        Config = rendererConfig
                    };
                    GC.Collect();
                }
            }
            if (isDisabled) ImGui.EndDisabled();

            if (ImGui.Button("Options", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                showOptions = true;
            }

            if (!isDisabled) ImGui.BeginDisabled();
            if (ImGui.Button("Abort", ImGui.GetContentRegionAvail() with { Y = 20 }))
                renderer?.Abort();
            if (!isDisabled) ImGui.EndDisabled();

            ImGui.Text($"State: {renderer?.State}");

            ImGui.NextColumn();

            var space = ImGui.GetContentRegionAvail();
            var ratio = space / new Vector2(preview.Width, preview.Height);
            var minRatio = MathF.Min(ratio.X, ratio.Y);

            if (renderer?.State is Renderer.RenderState.Lighting)
            {
                rlImGui_cs.rlImGui.ImageSize(renderer!.LightRenderer.Final.Texture, new Vector2(preview.Width, preview.Height) * minRatio);
            } 
            else
            {
                rlImGui_cs.rlImGui.ImageSize(preview.Raw.Texture, new Vector2(preview.Width, preview.Height) * minRatio);
            }
        }

        ImGui.End();

        if (showOptions) ImGui.OpenPopup("Render Options##RendererOptions");

        if (ImGui.BeginPopupModal("Render Options##RendererOptions", ImGuiWindowFlags.NoCollapse))
        {
            ImGui.Checkbox("Geometry output", ref rendererConfig.Geometry);

            ImGui.SeparatorText("Visuals");

            ImGui.Checkbox("Tiles", ref rendererConfig.Tiles);
            ImGui.Checkbox("Props", ref rendererConfig.Props);
            ImGui.Checkbox("Effects", ref rendererConfig.Effects);
            ImGui.Checkbox("Light", ref rendererConfig.Light);

            ImGui.SeparatorText("Cameras");

            ImGui.Checkbox("All", ref rendererConfig.AllCameras);

            if (rendererConfig.AllCameras) ImGui.BeginDisabled();
            for (var c = 0; c < level.Cameras.Count; c++)
            {
                var selected = rendererConfig.Cameras[c];

                if (ImGui.Checkbox($"Camera #{c+1}", ref selected))
                    rendererConfig.Cameras[c] = selected;
            }
            if (rendererConfig.AllCameras) ImGui.EndDisabled();

            if (ImGui.Button("Close"))
            {
                showOptions = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}