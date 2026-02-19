using Raylib_cs;
using static Raylib_cs.Raylib;

using System.Numerics;

namespace Tiler.Editor.Rendering;

/*
    RED   - Depth & Sunlight: 1 - 50 -> shadow, 51 - 100 -> sunlit
    GREEN - Value: 1 - 254 -> shadow - highlight
    BLUE  - Unspecified
*/

public class RenderEncoder
{
    public const int Width = 1400;
    public const int Height = 800;

    public readonly int LayerMargin = 100;

    public readonly Managed.RenderTexture[] Layers;
    public readonly Managed.RenderTexture Lightmap;

    public readonly Managed.RenderTexture Final;

    public readonly LevelCamera Camera;

    private readonly Managed.Shader Shader;

    public RenderEncoder(Managed.RenderTexture[] layers, Managed.RenderTexture lightmap, LevelCamera camera)
    {
        Layers = layers;
        Lightmap = lightmap;
        Camera = camera;

        Final = new Managed.RenderTexture(
            Width, 
            Height, 
            clearColor: new Color4(255, 255, 255, 255), 
            clear: true
        );

        Shader = new Managed.Shader(LoadShaderFromMemory(@"
#version 330

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

            """

            #version 330

            in vec2 fragTexCoord;
            in vec4 fragColor;

            uniform vec2 vertex_pos[4];
            uniform sampler2D texture0;
            uniform sampler2D lightmap;
            uniform int layer;

            out vec4 finalColor;

            float cross2d(vec2 a, vec2 b) {
                return a.x * b.y - a.y * b.x;
            }

            vec2 invbilinear_robust(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
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

            void main() {
                vec4 red = vec4(1, 0, 0, 1);
                vec4 green = vec4(0, 1, 0, 1);
                vec4 blue = vec4(0, 0, 1, 1);
                vec4 white = vec4(1, 1, 1, 1);

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
                
                // Final validation
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
                    discard;
                }

                uv.y = 1.0 - uv.y;

                vec4 pixel = texture(texture0, uv);
                vec4 light = texture(lightmap, uv);

                if (pixel.a < 1) { discard; }

                float value = 0;

                if (pixel == red) {
                    value = 0;
                } else if (pixel == green) {
                    value = 0.5;
                } else if (pixel == blue) {
                    value = 1;
                }

                int isSunlit = int(light == white);
                int isNotDark = int(!(layer == 0 && pixel == red));

                finalColor = vec4((isNotDark * (layer + 1)) / 255.0, value, float(isSunlit), 1);
            }
                    
            """));
    }

    public void Encode()
    {
        BeginTextureMode(Final);
        for (var l = Layers.Length - 1; l >= 0; l--)
        {
            BeginShaderMode(Shader);
            var layer = Layers[l];

            SetShaderValueTexture(
                Shader, 
                locIndex: GetShaderLocation(Shader, "texture0"), 
                texture:  layer.Texture
            );

            SetShaderValueTexture(
                Shader,
                locIndex: GetShaderLocation(Shader, "lightmap"),
                texture: Lightmap.Texture
            );

            SetShaderValue(
                Shader, 
                locIndex: GetShaderLocation(Shader, "layer"), 
                l, 
                ShaderUniformDataType.Int
            );

            var progress = l / (float)Layers.Length;

            // Initial positions

            var quad = new Quad(
                topLeft:     Vector2.Zero - (Vector2.One * LayerMargin),
                topRight:    (Vector2.UnitX * LevelCamera.Width) + new Vector2(LayerMargin, -LayerMargin),
                bottomRight: new Vector2(LevelCamera.Width + LayerMargin, LevelCamera.Height + LayerMargin),
                bottomLeft:  (Vector2.UnitY * LevelCamera.Height) + new Vector2(-LayerMargin, LayerMargin)
            );

            // Quad interpolation

            quad.TopLeft = Raymath.Vector2Lerp(
                v1: quad.TopLeft, 
                v2: quad.TopLeft + Camera.TopLeft.Position, 
                amount: progress
            );

            quad.TopRight = Raymath.Vector2Lerp(
                v1: quad.TopRight, 
                v2: quad.TopRight + Camera.TopRight.Position, 
                amount: progress
            );
            
            quad.BottomRight = Raymath.Vector2Lerp(
                v1: quad.BottomRight, 
                v2: quad.BottomRight + Camera.BottomRight.Position, 
                amount: progress
            );
            
            quad.BottomLeft = Raymath.Vector2Lerp(
                v1: quad.BottomLeft, 
                v2: quad.BottomLeft + Camera.BottomLeft.Position, 
                amount: progress
            );

            // Vertical flipping

            quad = new Quad(
                topLeft:     new Vector2(quad.BottomLeft.X, Final.Height - quad.BottomLeft.Y),
                topRight:    new Vector2(quad.BottomRight.X, Final.Height - quad.BottomRight.Y),
                bottomRight: new Vector2(quad.TopRight.X, Final.Height - quad.TopRight.Y),
                bottomLeft:  new Vector2(quad.TopLeft.X, Final.Height - quad.TopLeft.Y)
            );

            var quadArr = new Vector2[4]
            {
                quad.TopLeft,
                quad.TopRight,
                quad.BottomRight,
                quad.BottomLeft,
            };

            SetShaderValueV(
                Shader, 
                locIndex: GetShaderLocation(Shader, uniformName: "vertex_pos"), 
                quadArr, 
                ShaderUniformDataType.Vec2, 
                count: 4
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
}