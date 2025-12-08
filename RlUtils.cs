namespace Tiler.Editor;

using System.Numerics;
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
    public static void DrawTextureQuad(
        in Texture2D texture,
        in Rectangle source,
        in Quad quad
    )
    {
        Rlgl.SetTexture(texture.Id);

        Rlgl.Begin(DrawMode.Quads);

        Rlgl.Color4ub(255, 255, 255, 255);

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

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// rectangle avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Rectangle destination
    )
    {
        BeginTextureMode(rt);
        DrawTexturePro(
            texture,
            source,
            dest: new Rectangle(
                destination.X,
                rt.Texture.Height - destination.Height - destination.Y,
                destination.Width,
                destination.Height
            ),
            origin: Vector2.Zero,
            rotation: 0,
            tint: Color.White
        );
        EndTextureMode();
    }

    /// <summary>
    /// Draws a rectangular portion of a texture into a framebuffer inside a 
    /// quad avoiding the vertical flip of the drawing.
    /// </summary>
    public static void DrawTextureRT(
        in RenderTexture2D rt, 
        in Texture2D texture, 
        in Rectangle source, 
        in Quad destination
    )
    {
        Quad quad = new(
            topLeft:     new(destination.BottomLeft.X, rt.Texture.Height - destination.BottomLeft.Y),
            topRight:    new(destination.BottomRight.X, rt.Texture.Height - destination.BottomRight.Y),
            bottomRight: new(destination.TopRight.X, rt.Texture.Height - destination.TopRight.Y),
            bottomLeft:  new(destination.TopLeft.X, rt.Texture.Height - destination.TopLeft.Y)
        );

        BeginTextureMode(rt);
        DrawTextureQuad(texture, source, quad);
        EndTextureMode();
    }
}