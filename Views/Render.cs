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
            null, 
            @"
            #version 330
            
            uniform sampler2D texture0;
            uniform float tintAccum;

            in vec2 fragTexCoord;
            in vec4 fragColor;

            out vec4 finalColor;

            void main() {
                vec4 pixel = texture(texture0, fragTexCoord);
                if (pixel == vec4(1, 1, 1, 1)) { discard; }

                finalColor = clamp(vec4(pixel.r + tintAccum, pixel.g + tintAccum, pixel.b + tintAccum, pixel.a), vec4(0,0,0,0), vec4(1,1,1,1));
            }
            "
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

    private readonly Shader composeShader;
    // private readonly Shader vflipShader;

    private RenderTexture preview;

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

        BeginShaderMode(composeShader);
        for (int l = renderer.Layers.Length - 1; l >= 0; l--)
        {
            SetShaderValueTexture(
                shader:   composeShader, 
                locIndex: GetShaderLocation(composeShader, "texture0"), 
                texture:  renderer.Layers[l].Raw.Texture
            );

            float tint = l / (renderer.Layers.Length + 1.0f);

            SetShaderValue(
                shader:   composeShader,
                locIndex: GetShaderLocation(composeShader, "tintAccum"),
                value:    tint,
                ShaderUniformDataType.Float
            );

            RlUtils.DrawTextureRT(
                rt:          preview, 
                texture:     renderer.Layers[l].Raw.Texture, 
                source:      new Rectangle(renderer.LayerMargin, renderer.LayerMargin, Renderer.Width, Renderer.Height),
                destination: new Rectangle(-l + 6, -l + 6, preview.Width, preview.Height)
            );
        }
        EndShaderMode();
    }

    public override void GUI()
    {
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

            var isDisabled = renderer?.State is not null and not Renderer.RenderState.Done or Renderer.RenderState.Idle;
            if (isDisabled) ImGui.BeginDisabled();
            if (ImGui.Button("Start", ImGui.GetContentRegionAvail() with { Y = 20 }))
            {
                if (renderer is null or { State: Renderer.RenderState.Done }) 
                    renderer = new(Context.SelectedLevel!, Context.Tiles);
            }
            if (isDisabled) ImGui.EndDisabled();

            ImGui.Text($"State: {renderer?.State}");

            ImGui.NextColumn();

            var space = ImGui.GetContentRegionAvail();
            var ratio = space / new Vector2(preview.Width, preview.Height);
            var minRatio = MathF.Min(ratio.X, ratio.Y);

            // rlImGui_cs.rlImGui.ImageRenderTextureFit(preview, false);
            rlImGui_cs.rlImGui.ImageSize(preview.Raw.Texture, new Vector2(preview.Width, preview.Height) * minRatio);
            // if (renderer is not null) rlImGui_cs.rlImGui.ImageRenderTextureFit(renderer.Layers[0], false);
        }

        ImGui.End();
    }
}