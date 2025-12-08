namespace Tiler.Editor;

using Tiler.Editor.Managed;

public abstract class Tileset
{
    public Image Image { get; set; }

    public Tileset(Image image)
    {
        Image = image;
    }

    public Tileset(Raylib_cs.Image image)
    {
        Image = new(image);
    }

    public abstract Raylib_cs.Rectangle GetRect(int x, int y);
}