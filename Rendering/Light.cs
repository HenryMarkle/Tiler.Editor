using System.Collections.Generic;
using System.Linq;
using Tiler.Editor.Rendering.Scripting;
using Tiler.Editor.Tile;
using Tiler.Editor.Managed;
using Raylib_cs;
using System.Numerics;
using System;

namespace Tiler.Editor.Rendering;

public class LightRenderer
{
    public const int Width = 1400 + 100*2;
    public const int Height = 800 + 100*2;

    public RenderTexture Commulative { get; private set; }
    public RenderTexture Final { get; private set; }
    public RenderTexture[] Layers { get; init; }

    private Raylib_cs.Shader SilhouetteShader { get; init; }
    private Raylib_cs.Shader MaskShader { get; init; }

    private float Distance { get; init; }
    private int Direction { get; init; }

    public LightRenderer(RenderTexture[] layers, float distance, int direction)
    {
        Layers = layers;
        Distance = distance;
        Direction = direction;

        Commulative = new RenderTexture(Width, Height, clearColor: new Color4(255, 255, 255, 255), clear: true);
        Final = new RenderTexture(Width, Height, clearColor: new Color4(0, 0, 0, 255), clear: true);

        SilhouetteShader = Raylib.LoadShaderFromMemory(
            null,
            @"#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;

out vec4 finalColor;

void main() {
    vec4 white = vec4(1, 1, 1, 1);
    vec4 black = vec4(0, 0, 0, 1);

    vec4 pixel = texture(texture0, fragTexCoord);

    if (pixel == vec4(0, 0, 0, 0)) {
        discard;
    } else {
        finalColor = black;
    }
}"
        );

        MaskShader = Raylib.LoadShaderFromMemory(
            null,
            @"#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform sampler2D mask0;

out vec4 finalColor;

void main() {
    vec4 white = vec4(1, 1, 1, 1);
    vec4 black = vec4(0, 0, 0, 1);

    vec4 pixel = texture(texture0, fragTexCoord);
    vec4 maskPixel = texture(mask0, fragTexCoord);

    if (pixel == vec4(0, 0, 0, 0)) { discard; }
    if (maskPixel == black) { discard; }

    else finalColor = white;
}"
        );
    }

    ~LightRenderer()
    {
        Raylib.UnloadShader(SilhouetteShader);
        Raylib.UnloadShader(MaskShader);
    }

    //

    public bool IsDone { get; private set; }

    public int Progress { get; private set; }

    public void Next()
    {
        if (Progress >= Layers.Length)
        {
            IsDone = true;
            return;
        }

        //

        var layer = Layers[Progress];
        var projection = new Vector2(
            -MathF.Cos(float.DegreesToRadians(Direction)),
            MathF.Sin(float.DegreesToRadians(Direction))
        );

        projection = Raymath.Vector2Normalize(projection);
        projection = (projection * Progress) + (projection * Distance * 10);

        Raylib.BeginTextureMode(Final);
        Raylib.BeginShaderMode(MaskShader);
        {
            Raylib.SetShaderValueTexture(
                shader:   MaskShader, 
                locIndex: Raylib.GetShaderLocation(MaskShader, "texture0"), 
                texture:  layer.Texture
            );
            Raylib.SetShaderValueTexture(
                shader:   MaskShader, 
                locIndex: Raylib.GetShaderLocation(MaskShader, "mask0"), 
                texture:  Commulative.Texture
            );

            // Note: Does not work with more than 1 sampler (texture)
            // RlUtils.DrawTextureRT(
            //     rt:          Final, 
            //     texture:     layer.Texture,
            //     source:      new Rectangle(0, 0, layer.Width, layer.Height),
            //     destination: new Rectangle(0, 0, layer.Width, layer.Height)
            // );

            var rect = new Rectangle(0, 0, layer.Width, layer.Height);
        
            Raylib.DrawTexturePro(
                layer.Texture,
                source: rect with { Y = rect.Y + rect.Height, Height = -rect.Height },
                dest: new Rectangle(
                    rect.X,
                    Final.Texture.Height - rect.Height - rect.Y,
                    rect.Width,
                    rect.Height
                ),
                origin: Vector2.Zero,
                rotation: 0,
                Color.White
            );
        }
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        Raylib.BeginShaderMode(SilhouetteShader);
        Raylib.SetShaderValueTexture(
            shader:   SilhouetteShader, 
            locIndex: Raylib.GetShaderLocation(SilhouetteShader, "texture0"), 
            texture:  layer.Texture
        );
        RlUtils.DrawTextureRT(
            rt:          Commulative, 
            texture:     layer.Texture,
            source:      new Rectangle(0, 0, layer.Width, layer.Height),
            destination: new Rectangle(projection.X, projection.Y, layer.Width, layer.Height)
        );
        Raylib.EndShaderMode();

        Progress++;
    }
}