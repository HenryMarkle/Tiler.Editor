using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Serilog;
using Tiler.Editor.Rendering.Scripting;
using Tiler.Editor.Tile;

namespace Tiler.Editor.Rendering;

public class PropRenderer
{
    private const int Width = 1400;
    private const int Height = 800;

    private readonly Managed.RenderTexture[] layers;
    public readonly Level level;
    private readonly PropDex props;
    private readonly int sublayersPerLayer;
    private readonly int layerMargin;
    private readonly LevelCamera camera;

    private readonly List<Prop> propsToDraw;
    private int progress;

    private readonly Shader invbShader;
    private readonly Shader softShader;
    private readonly Shader antimatterShader;


    public bool IsDone { get; private set; }


    public PropRenderer(Managed.RenderTexture[] layers, Level level, PropDex props, LevelCamera camera)
    {
        this.layers = layers;
        this.level = level;
        this.props = props;
        this.camera = camera;
        layerMargin = 100;
        sublayersPerLayer = 10;
        propsToDraw = [];
        IsDone = false;

        invbShader = Raylib.LoadShaderFromMemory(
            @"#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;

out vec2 fragTexCoord;
out vec4 fragColor;

uniform vec2 vertex_pos[4];

void main()
{
    fragTexCoord = vertexPosition.xy;
    
    fragColor = vertexColor;

    gl_Position = mvp*vec4(vertexPosition, 1.0);
}",
            @"// Inverse-Bilinear Interpolation fragment shader.
// This took me forever.

#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec2 vertex_pos[4];
uniform float tex_source_pos[4];
uniform int vflip;
uniform sampler2D texture0;

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
    if (bool(vflip)) {
        uv.y = 1.0 - uv.y;
    }

    uv.x = tex_source_pos[0] + uv.x*(tex_source_pos[2] - tex_source_pos[0]);
    uv.y = tex_source_pos[1] + uv.y*(tex_source_pos[3] - tex_source_pos[1]);

    
    // Fallback to iterative if robust method fails
    const float TOLERANCE = 0.52; // Slightly more than 0.5 to account for boundary pixels
    if (max(abs(uv.x - 0.5), abs(uv.y - 0.5)) > TOLERANCE) {
        uv = invbilinear_iterative(fragTexCoord, va, vb, vc, vd);
    }
    
    // Final validation
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
        discard;
    }
    
    vec4 c = texture(texture0, uv);
    finalColor = c * fragColor;
}"
        );

        softShader = Raylib.LoadShaderFromMemory(
            null,
            @"
            #version 330

            in vec2 fragTexCoord;
            in vec4 fragColor;

            uniform sampler2D texture0;

            uniform vec2 texture_size;
            uniform int vflip; // 0, 1
            uniform int effect_color; // 0, 1, 2
            uniform int self_shade; // 0, 1
            uniform int smooth_shading;
            uniform float depth_affecthilites;
            uniform float highlight_border;
            uniform float shadow_border;
            uniform float contour_exp;
            uniform float depth; // 0 - 1

            out vec4 finalColor;

            float get_depth(vec2 pos) {
                return texture(texture0, pos).g;
            }

            void main()
            {
                vec2 uv = fragTexCoord;
                vec2 one = vec2(1, 1) / texture_size;
                vec4 white = vec4(1, 1, 1, 1);
                vec4 black = vec4(0, 0, 0, 1);
                vec4 trans = vec4(0, 0, 0, 0);

                // if (bool(vflip)) { uv.y = 1.0 - uv.y; }

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
                    discard;
                }

                vec4 pixel = texture(texture0, uv);

                if (depth > pixel.g) { discard; }

                if (pixel == white || pixel == black || pixel == trans) { discard; }
                
                float pixel_depth = pixel.g;

                vec3 pal_color = vec3(0, 1, 0);

                float dirmod = 1;
                if (bool(vflip)) dirmod = -1;

                if (effect_color == 1) {
                    pal_color = vec3(1, 0, 1);
                } else if (effect_color == 2) {
                    pal_color = vec3(0, 1, 1); 
                }

                if (self_shade == 0) {
                    if (effect_color == 1) {
                        if (pixel.b > 1.0/3.0*2) {
                            pal_color = vec3(1, 150/255.0, 1);
                        } else if (pixel.b < 1.0/3.0) {
                            pal_color = vec3(150/255.0, 0, 150/255.0);
                        }
                    } else if (effect_color == 2) {
                        if (pixel.b > 1.0/3.0*2) {
                            pal_color = vec3(150/255.0, 1, 1);
                        } else if (pixel.b < 1.0/3.0) {
                            pal_color = vec3(0,150/255.0,150/255.0);
                        }
                    } else {
                        if (pixel.b > 1.0/3.0*2) {
                            pal_color = vec3(0, 0 ,1);
                        } else if (pixel.b < 1.0/3.0) {
                            pal_color = vec3(1, 0, 0);
                        }
                    }
                }

                float ang = 0;

                for (int a = 1; a <= smooth_shading; a++) {
                    // iteration 1
                    vec2 point = vec2(1, 0) * one;

                    ang += (pixel_depth - get_depth(uv - point*a) + (get_depth(uv + point*a) - depth));

                    // iteration 2
                    point = vec2(1, 1) * one;

                    ang += (pixel_depth - get_depth(uv - point*a) + (get_depth(uv + point*a) - depth));

                    // iteration 3
                    point = vec2(0, 1) * one;

                    ang += (pixel_depth - get_depth(uv - point*a) + (get_depth(uv + point*a) - depth));
                }

                ang /= smooth_shading * 3;

                ang *= 1 - pixel.r;

                if (ang * 10 * pow(pixel_depth, depth_affecthilites) > highlight_border) {
                    if (effect_color == 1) {
                        pal_color = vec3(1, 150/255.0, 1);
                    } else if (effect_color == 2) {
                        pal_color = vec3(150/255.0, 1, 1);
                    } else {
                        pal_color = vec3(0, 0, 1);
                    }
                } else if (-ang*10.0 > shadow_border) {
                    if (effect_color == 1) {
                        pal_color = vec3(150/255.0, 0, 150/255.0);
                    } else if (effect_color == 2) {
                        pal_color = vec3(0, 150/255.0, 150/255.0);
                    } else {
                        pal_color = vec3(1, 0, 0);
                    }
                }

                finalColor = vec4(pal_color, 1);
            }

            "
        );

        antimatterShader = Raylib.LoadShaderFromMemory(
            null,
            @"
            #version 330

            in vec2 fragTexCoord;
            in vec4 fragColor;

            uniform sampler2D texture0;

            uniform int vflip; // 0, 1
            uniform float depth; // 0 - 1

            out vec4 finalColor;

            void main()
            {
                vec2 uv = fragTexCoord;
                vec4 white = vec4(1, 1, 1, 1);
                vec4 black = vec4(0, 0, 0, 1);
                vec4 trans = vec4(0, 0, 0, 0);

                // if (bool(vflip)) { uv.y = 1.0 - uv.y; }

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
                    discard;
                }

                vec4 pixel = texture(texture0, uv);

                if (depth > pixel.a) { discard; }

                if (pixel == trans) { discard; }
                
                float pixel_depth = pixel.a;

                finalColor = vec4(0, 0, 0, 0);
            }

            "
        );

        var columns = Width + layerMargin*2;
        var rows = Height + layerMargin*2;

        var camPos = camera.Position - (Vector2.One*layerMargin);
        var camRec = new Rectangle(camPos, new Vector2(columns, rows));

        // Ignore out-of-bounds props

        foreach (var prop in level.Props)
        {
            if (Raylib.CheckCollisionRecs(prop.Quad.Enclosed(), camRec)) 
                propsToDraw.Add(prop);
        }
    }

    ~PropRenderer()
    {
        Raylib.UnloadShader(invbShader);
        Raylib.UnloadShader(softShader);
        Raylib.UnloadShader(antimatterShader);
    }

    // NOTE: temporary
    public void Next()
    {
        int cap = 0;
        while (progress < propsToDraw.Count && cap++ < 30)
        {
            var prop = propsToDraw[progress];

            if (prop.Depth >= layers.Length) continue;

            switch (prop.Def)
            {
                case VoxelStruct voxels:
                    {
                        voxels.Image.ToTexture();

                        var quad = new Quad(prop.Quad) + (Vector2.One * layerMargin) - camera.Position;

                        quad = new Quad(
                            topLeft:     new(quad.BottomLeft.X, layers[0].Texture.Height - quad.BottomLeft.Y),
                            topRight:    new(quad.BottomRight.X, layers[0].Texture.Height - quad.BottomRight.Y),
                            bottomRight: new(quad.TopRight.X, layers[0].Texture.Height - quad.TopRight.Y),
                            bottomLeft:  new(quad.TopLeft.X, layers[0].Texture.Height - quad.TopLeft.Y)
                        );

                        var vertices = new Vector2[4]
                        {
                            quad.TopLeft,
                            quad.TopRight,
                            quad.BottomRight,
                            quad.BottomLeft,
                        };

                        int vflip = 1;

                        Raylib.BeginShaderMode(invbShader);
                        Raylib.SetShaderValueTexture(
                            shader:   invbShader, 
                            locIndex: Raylib.GetShaderLocation(invbShader, "texture0"), 
                            texture:  voxels.Image
                        );

                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vertex_pos"), 
                            values:      vertices, 
                            uniformType: ShaderUniformDataType.Vec2, 
                            count:       4
                        );

                        Raylib.SetShaderValue(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vflip"), 
                            value:       vflip,
                            uniformType: ShaderUniformDataType.Int
                        );

                        for (var l = 0; l < voxels.Layers; l++)
                        {
                            if (prop.Depth + l >= layers.Length) break;
                            
                            for (var r = 0; r < voxels.Repeat[l]; r++)
                            {
                                if (prop.Depth + l + r >= layers.Length) break;

                                Raylib.SetShaderValueV(
                                    shader:      invbShader, 
                                    locIndex:    Raylib.GetShaderLocation(invbShader, "tex_source_pos"), 
                                    values:      new float[4] { 
                                                     0, 
                                                     (float)(l * voxels.Height) / voxels.Image.Height, 
                                                     (float)voxels.Width / voxels.Image.Width, 
                                                     (float)voxels.Height / voxels.Image.Height 
                                                 },
                                    uniformType: ShaderUniformDataType.Float, 
                                    count:       4
                                );

                                // Does not seem to work with the shader

                                // RlUtils.DrawTextureRT(
                                //     rt: layers[l + r],
                                //     texture: voxels.Image,
                                //     source: new Rectangle(0, l * voxels.Height, voxels.Width, voxels.Height),
                                //     // source: new Rectangle(0, 0, voxels.Image.Width, voxels.Image.Height),
                                //     destination: prop.Quad
                                // );

                                Raylib.BeginTextureMode(layers[prop.Depth + l + r]);
                                RlUtils.DrawTextureQuad(
                                    texture: voxels.Image,
                                    source:  new Rectangle(0, l * voxels.Height, voxels.Width, voxels.Height),
                                    quad:    quad
                                );
                                Raylib.EndTextureMode();
                            }
                        }
                        Raylib.EndShaderMode();
                    }
                    break;

                case Soft soft:
                    {
                        soft.Image.ToTexture();

                        var enclosing = prop.Quad.Enclosed();
                        using var quadifiedRT = new Managed.RenderTexture(
                            (int)enclosing.Width, 
                            (int)enclosing.Height, 
                            new Color4(0,0,0,0), 
                            true
                        );

                        var quad = new Quad(prop.Quad) - enclosing.Position;

                        quad = new Quad(
                            topLeft:     new(quad.BottomLeft.X, enclosing.Height - quad.BottomLeft.Y),
                            topRight:    new(quad.BottomRight.X, enclosing.Height - quad.BottomRight.Y),
                            bottomRight: new(quad.TopRight.X, enclosing.Height - quad.TopRight.Y),
                            bottomLeft:  new(quad.TopLeft.X, enclosing.Height - quad.TopLeft.Y)
                        );

                        var vertices = new Vector2[4]
                        {
                            quad.TopLeft,
                            quad.TopRight,
                            quad.BottomRight,
                            quad.BottomLeft,
                        };

                        int vflip = 1;

                        Raylib.BeginShaderMode(invbShader);
                        Raylib.SetShaderValueTexture(
                            shader:   invbShader, 
                            locIndex: Raylib.GetShaderLocation(invbShader, "texture0"), 
                            texture:  soft.Image
                        );

                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vertex_pos"), 
                            values:      vertices, 
                            uniformType: ShaderUniformDataType.Vec2, 
                            count:       4
                        );

                        Raylib.SetShaderValue(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vflip"), 
                            value:       vflip,
                            uniformType: ShaderUniformDataType.Int
                        );

                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "tex_source_pos"), 
                            values:      new float[4] { 0, 0, 1, 1 },
                            uniformType: ShaderUniformDataType.Float, 
                            count:       4
                        );

                        Raylib.BeginTextureMode(quadifiedRT);
                        RlUtils.DrawTextureQuad(
                            texture: soft.Image,
                            source:  new Rectangle(0, 0, soft.Width, soft.Height),
                            quad:    quad
                        );
                        Raylib.EndTextureMode();
                        Raylib.EndShaderMode();

                        vflip = 0;

                        Raylib.BeginShaderMode(softShader);
                        Raylib.SetShaderValueTexture(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "texture0"),
                            texture: quadifiedRT.Texture
                        );
                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "texture_size"), 
                            values:      new float[2] { quadifiedRT.Width, quadifiedRT.Height }, 
                            uniformType: ShaderUniformDataType.Vec2, 
                            count:       1
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "vflip"),
                            value: vflip,
                            uniformType: ShaderUniformDataType.Int
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "effect_color"),
                            value: (int)soft.EffectColor,
                            uniformType: ShaderUniformDataType.Int
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "self_shade"),
                            value: soft.SelfShade,
                            uniformType: ShaderUniformDataType.Int
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "smooth_shading"),
                            value: soft.SmoothShading,
                            uniformType: ShaderUniformDataType.Int
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "depth_affecthilites"),
                            value: soft.DepthAffectsHighlights,
                            uniformType: ShaderUniformDataType.Float
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "highlight_border"),
                            value: soft.HighlightBorder,
                            uniformType: ShaderUniformDataType.Float
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "shadow_border"),
                            value: soft.ShadowBorder,
                            uniformType: ShaderUniformDataType.Float
                        );
                        Raylib.SetShaderValue(
                            shader: softShader,
                            locIndex: Raylib.GetShaderLocation(softShader, "contour_exp"),
                            value: soft.ContourExp,
                            uniformType: ShaderUniformDataType.Float
                        );

                        for (var d = 0; d < ((SoftConfig)prop.Config).Depth; d++)
                        {
                            if (d + prop.Depth >= layers.Length) break;

                            Raylib.SetShaderValue(
                                shader: softShader,
                                locIndex: Raylib.GetShaderLocation(softShader, "depth"),
                                value: (float)d / ((SoftConfig)prop.Config).Depth,
                                uniformType: ShaderUniformDataType.Float
                            );
                            RlUtils.DrawTextureRT(
                                rt: layers[d + prop.Depth],
                                texture: quadifiedRT.Texture,
                                source: new Rectangle(0, 0, quadifiedRT.Width, quadifiedRT.Height),
                                destination: enclosing with { Position = enclosing.Position + (Vector2.One * layerMargin) - camera.Position }
                            );
                        }
                        Raylib.EndShaderMode();
                    }
                    break;

                case Antimatter antimatter:
                    {
                        antimatter.Image.ToTexture();

                        var enclosing = prop.Quad.Enclosed();
                        using var quadifiedRT = new Managed.RenderTexture(
                            (int)enclosing.Width, 
                            (int)enclosing.Height, 
                            new Color4(0,0,0,0), 
                            true
                        );

                        var quad = new Quad(prop.Quad) - enclosing.Position;

                        quad = new Quad(
                            topLeft:     new(quad.BottomLeft.X, enclosing.Height - quad.BottomLeft.Y),
                            topRight:    new(quad.BottomRight.X, enclosing.Height - quad.BottomRight.Y),
                            bottomRight: new(quad.TopRight.X, enclosing.Height - quad.TopRight.Y),
                            bottomLeft:  new(quad.TopLeft.X, enclosing.Height - quad.TopLeft.Y)
                        );

                        var vertices = new Vector2[4]
                        {
                            quad.TopLeft,
                            quad.TopRight,
                            quad.BottomRight,
                            quad.BottomLeft,
                        };

                        int vflip = 1;

                        Raylib.BeginShaderMode(invbShader);
                        Raylib.SetShaderValueTexture(
                            shader:   invbShader, 
                            locIndex: Raylib.GetShaderLocation(invbShader, "texture0"), 
                            texture:  antimatter.Image
                        );

                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vertex_pos"), 
                            values:      vertices, 
                            uniformType: ShaderUniformDataType.Vec2, 
                            count:       4
                        );

                        Raylib.SetShaderValue(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "vflip"), 
                            value:       vflip,
                            uniformType: ShaderUniformDataType.Int
                        );

                        Raylib.SetShaderValueV(
                            shader:      invbShader, 
                            locIndex:    Raylib.GetShaderLocation(invbShader, "tex_source_pos"), 
                            values:      new float[4] { 0, 0, 1, 1 },
                            uniformType: ShaderUniformDataType.Float, 
                            count:       4
                        );

                        Raylib.BeginTextureMode(quadifiedRT);
                        RlUtils.DrawTextureQuad(
                            texture: antimatter.Image,
                            source:  new Rectangle(0, 0, antimatter.Width, antimatter.Height),
                            quad:    quad
                        );
                        Raylib.EndTextureMode();
                        Raylib.EndShaderMode();

                        vflip = 0;

                        Rlgl.SetBlendMode(BlendMode.Custom);

                        Rlgl.SetBlendFactors(1, 0, 1);

                        Raylib.BeginShaderMode(antimatterShader);
                        Raylib.SetShaderValueTexture(
                            shader: antimatterShader,
                            locIndex: Raylib.GetShaderLocation(antimatterShader, "texture0"),
                            texture: quadifiedRT.Texture
                        );
                        Raylib.SetShaderValue(
                            shader: antimatterShader,
                            locIndex: Raylib.GetShaderLocation(antimatterShader, "vflip"),
                            value: vflip,
                            uniformType: ShaderUniformDataType.Int
                        );

                        for (var d = 0; d < ((AntimatterConfig)prop.Config).Depth; d++)
                        {
                            if (d + prop.Depth >= layers.Length) break;

                            Raylib.SetShaderValue(
                                shader: antimatterShader,
                                locIndex: Raylib.GetShaderLocation(antimatterShader, "depth"),
                                value: (float)d / ((AntimatterConfig)prop.Config).Depth,
                                uniformType: ShaderUniformDataType.Float
                            );
                            RlUtils.DrawTextureRT(
                                rt: layers[d + prop.Depth],
                                texture: quadifiedRT.Texture,
                                source: new Rectangle(0, 0, quadifiedRT.Width, quadifiedRT.Height),
                                destination: enclosing with { Position = enclosing.Position + (Vector2.One * layerMargin) - camera.Position }
                            );
                        }
                        Raylib.EndShaderMode();
                        Raylib.EndBlendMode();
                    }
                    break;

                case Custom custom:
                    {
                        
                    }
                    break;
            }

            progress++;
        }

        IsDone = progress == propsToDraw.Count;
    }
}