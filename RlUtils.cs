namespace Tiler.Editor;

using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

public static class RlUtils
{
    public static void DrawTextureQuad(
        in Texture2D texture,
        in Rectangle source,
        in Quad quad,
        Color4 tint
    )
    {
        Rlgl.SetTexture(texture.Id);

        Rlgl.Begin(DrawMode.Quads);

        Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);

        Rlgl.TexCoord2f((source.X + source.Width) / texture.Width, source.Y / texture.Height);
        Rlgl.Vertex2f(quad.TopRight.X, quad.TopRight.Y);
     
        Rlgl.TexCoord2f(source.X / texture.Width, source.Y / texture.Height);
        Rlgl.Vertex2f(quad.TopLeft.X, quad.TopLeft.Y);
     
        Rlgl.TexCoord2f(source.X / texture.Width, (source.Y + source.Height) / texture.Height);
        Rlgl.Vertex2f(quad.BottomLeft.X, quad.BottomLeft.Y);
     
        Rlgl.TexCoord2f((source.X + source.Width) / texture.Width, (source.Y + source.Height) / texture.Height);
        Rlgl.Vertex2f(quad.BottomRight.X, quad.BottomRight.Y);
    
        Rlgl.End();

        Rlgl.SetTexture(0);
    }

    public static void DrawTextureTriangle(
        in Texture2D texture,
        in Triangle source,
        in Triangle triangle,
        Color4 tint
    )
    {
        Rlgl.SetTexture(texture.Id);

        Rlgl.Begin(DrawMode.Triangles);

        Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);

        var width = texture.Width;
        var height = texture.Height;

        Rlgl.TexCoord2f(source.A.X / width, source.A.Y / height);
        Rlgl.Vertex2f(triangle.A.X, triangle.A.Y);
     
        Rlgl.TexCoord2f(source.B.X / width, source.B.Y / height);
        Rlgl.Vertex2f(triangle.B.X, triangle.B.Y);
     
        Rlgl.TexCoord2f(source.C.X / width, source.C.Y / height);
        Rlgl.Vertex2f(triangle.C.X, triangle.C.Y);
    
        Rlgl.End();

        Rlgl.SetTexture(id: 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextureQuad(
        in Texture2D texture,
        in Rectangle source,
        in Quad quad
    ) => DrawTextureQuad(texture, source, quad, new Color4(255, 255, 255, 255));

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// rectangle avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Rectangle destination,
        Color4 tint
    )
    {
        BeginTextureMode(rt);
        DrawTexturePro(
            texture,
            source: source with { Height = -source.Height },
            dest: new Rectangle(
                destination.X,
                rt.Texture.Height - destination.Height - destination.Y,
                destination.Width,
                destination.Height
            ),
            origin: Vector2.Zero,
            rotation: 0,
            tint
        );
        EndTextureMode();
    }

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// rectangle avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureTriangleRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Triangle source, 
        in Triangle destination,
        Color4 tint
    )
    {
        BeginTextureMode(rt);
        DrawTextureTriangle(
            texture,
            source,
            triangle: new Triangle(
                destination.A with { Y = rt.Texture.Height - destination.A.Y },
                destination.C with { Y = rt.Texture.Height - destination.C.Y },
                destination.B with { Y = rt.Texture.Height - destination.B.Y }
                ),
            // triangle: destination,
            tint);
        EndTextureMode();
    }

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// rectangle avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Rectangle destination,
        in Vector2 origin,
        float rotation,
        Color4 tint
    )
    {
        BeginTextureMode(rt);
        DrawTexturePro(
            texture,
            source: source with { Y = source.Y + source.Height, Height = -source.Height },
            dest: new Rectangle(
                destination.X,
                rt.Texture.Height - destination.Y,
                destination.Width,
                -destination.Height
            ),
            origin: origin with { Y = destination.Height - origin.Y },
            rotation: -rotation,
            tint
        );
        EndTextureMode();
    }

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// rectangle avoiding the vertical flip of the drawing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Rectangle destination
    ) => DrawTextureRT(rt, texture, source, destination, Color.White);

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// quad avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Quad destination,
        Color4 tint
    )
    {
        Quad quad = new(
            topLeft:     new(destination.BottomLeft.X, rt.Texture.Height - destination.BottomLeft.Y),
            topRight:    new(destination.BottomRight.X, rt.Texture.Height - destination.BottomRight.Y),
            bottomRight: new(destination.TopRight.X, rt.Texture.Height - destination.TopRight.Y),
            bottomLeft:  new(destination.TopLeft.X, rt.Texture.Height - destination.TopLeft.Y)
        );

        BeginTextureMode(rt);
        DrawTextureQuad(
            texture, 
            source: source with { Y = source.Y + source.Height, Height = -source.Height }, 
            quad, 
            tint
        );
        EndTextureMode();
    }

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// quad avoiding the vertical flip of the drawing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Quad destination
    ) => DrawTextureRT(rt, texture, source, destination, Color.White);
}